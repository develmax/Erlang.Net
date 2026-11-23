using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DeferOwnedTerminalRecordTail(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || !code.Span.Contains(UpdateRecord) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        var allocations = new HashSet<int>();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int heap = start + 1; heap + 6 < end; heap++)
            {
                if (body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                    || body[heap - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[heap + 1] is not { Definition.Opcode: PutTuple, Operands: [var tuple, _, ..] } build
                    || tuple != x0 || build.Operands.Any(value => SameProjectionRegister(value, x1))
                    || body[heap + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedHead]
                    } || result != x0
                    || body[heap + 3] is not
                    {
                        Definition.Opcode: PutList,
                        Operands: [var head, { Kind: BeamOperandParameters.Y, TypeIndex: null } tail, var list]
                    } || head != savedHead || tail == savedHead || list != x0
                    || body[heap + 4] is not { Definition.Opcode: UpdateRecord, Operands: [_, _, var state, var update, _, ..] } record
                    || update != x0 || state.Kind != BeamOperandParameters.Y
                    || record.Operands.Any(value => SameProjectionRegister(value, savedHead) || SameProjectionRegister(value, x1))
                    || body[heap + 5].Definition.Opcode != Deallocate || body[heap + 6].Definition.Opcode != Return
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frame)
                {
                    continue;
                }

                var (owners, uses) = frame;
                int allocation = owners[heap - start];
                if (allocation <= start || body[allocation].Offset < copied
                    || !uses.ContainsKey(allocation)
                    || body[allocation].Definition.Opcode != Allocate
                    || ReadOwnedTerminalTail(
                        body,
                        start,
                        end,
                        allocation,
                        heap,
                        owners,
                        state with { TypeIndex = null },
                        tail
                    ) is not { } projection)
                {
                    continue;
                }

                int allocationOffset = body[allocation].Offset;
                output.Write(code.Span[copied..allocationOffset]);
                allocations.Add((int)output.Length);
                output.Write(code.Span[allocationOffset..body[projection].Offset]);
                output.Write(code.Span[body[projection + 1].Offset..body[heap + 1].Offset]);
                // The record is still rooted, and the call has left only
                // X0 live. Neither constructor needs the old X1 value.
                output.WriteByte(GetTupleElement);
                WriteRegister(output, state);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[projection].Operands[1].Value);
                WriteRegister(output, x1);
                output.Write(code.Span[body[heap + 1].Offset..body[heap + 2].Offset]);
                output.WriteByte(PutList);
                WriteRegister(output, x0);
                WriteRegister(output, x1);
                WriteRegister(output, x0);
                copied = body[heap + 4].Offset;
                heap += 6;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        // Publish together with removal or reuse of the retired field slot.
        return CompactFramesBesideSiblingTrims(output.ToArray(), labelCount, allocations);
    }

    private static int? ReadOwnedTerminalTail(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int allocation,
        int heap,
        int[] owners,
        BeamOperand carrier,
        BeamOperand tail
    )
    {
        if (SameProjectionRegister(carrier, tail)
            || body[allocation - 1] is not
            { Definition.Opcode: IsTaggedTuple, Operands: [_, var argument, var width, _] }
            || !IsBareX(argument) || Enumerable.Range(heap + 1, 5).Any(index => owners[index - start] != allocation))
        {
            return null;
        }

        int startSave = allocation + 1;
        while (startSave < heap && (body[startSave].Definition.Opcode == InitYRegisters
            || body[startSave] is
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y }]
            } && !SameProjectionRegister(body[startSave].Operands[1], carrier)))
        {
            startSave++;
        }

        if (!IsMove(body[startSave], argument, carrier))
        {
            return null;
        }

        int prefixEnd = startSave + 1;
        while (prefixEnd < heap && body[prefixEnd].Definition.Opcode is (GetTupleElement or Move))
        {
            prefixEnd++;
        }

        int projection = -1;
        bool passedCall = false;
        for (int index = allocation + 1; index < end; index++)
        {
            if (owners[index - start] != allocation)
            {
                continue;
            }

            BeamInstruction instruction = body[index];
            if (index > startSave && (WritesDeferredTupleSlot(instruction, carrier)
                || instruction.Definition.Opcode == GetList && SameProjectionRegister(instruction.Operands[2], carrier)
                || StackWriteOperand(instruction) == int.MinValue && !IsEntryFramePredicate(instruction.Definition.Opcode)
                    && instruction.Operands.Any(value => SameProjectionRegister(value, carrier))))
            {
                return null;
            }

            if (index == heap + 3 || !instruction.Operands.Any(value => SameProjectionRegister(value, tail)))
            {
                if (index > projection && projection >= 0 && index < heap
                    && instruction.Definition.Opcode is (LocalCall or ExternalCall))
                {
                    passedCall = true;
                }

                continue;
            }

            if (instruction.Definition.Opcode == InitYRegisters && index < startSave)
            {
                continue;
            }

            if (projection >= 0 || index <= startSave || index >= prefixEnd
                || instruction is not
                { Definition.Opcode: GetTupleElement, Operands: [var tuple, var field, var destination] }
                || !SameProjectionRegister(tuple, carrier) || field.Value >= width.Value || destination != tail)
            {
                return null;
            }

            projection = index;
        }

        return passedCall ? projection : null;
    }
}
