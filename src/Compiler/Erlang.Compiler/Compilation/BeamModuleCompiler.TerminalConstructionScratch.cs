using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardTerminalConstructionScratch(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(UpdateRecord) || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteTerminalConstructionScratch(code, decoded.Body, decoded.StackOperands);
    }

    private byte[]? ForwardRetiredRecordConsCopy(ReadOnlyMemory<byte> code, int labelCount)
    {
        int listType = typeBits.IndexOf((ProperListTypeBits, null, null, null));
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(UpdateRecord) || !code.Span.Contains(TestHeap)
            || !atoms.TryGetValue("lists", out int lists)
            || !atoms.TryGetValue("reverse", out int reverse)
            || !atoms.TryGetValue(ReuseRecordUpdateHint, out int reuse)
            || !imports.TryGetValue((lists, reverse, 1), out int index)
            || listType < 0
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteTerminalConstructionScratchCore(
            code,
            decoded.Body,
            decoded.StackOperands,
            (emittedImports?[index] ?? index, MapEmittedAtom(reuse), listType)
        );
    }

    private static byte[]? RewriteTerminalConstructionScratch(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack
    ) => RewriteTerminalConstructionScratchCore(
        code,
        body,
        stack,
        retiredRecord: null
    );

    private static byte[]? RewriteTerminalConstructionScratchCore(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack,
        (int Reverse, int Reuse, int ListType)? retiredRecord
    )
    {
        var edits = new List<CodeEdit>();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        bool unprotected = false;
        for (int heap = 0; heap + 4 < body.Count; heap++)
        {
            if (body[heap].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, heap + 1);
                continue;
            }

            if (!unprotected || body[heap].Definition.Opcode != TestHeap)
            {
                continue;
            }

            int end = heap + 1;
            bool updatesRecord = false;
            while (end < body.Count && body[end].Definition.Opcode is
                (Move or GetTupleElement or PutTuple or PutList or UpdateRecord))
            {
                updatesRecord |= body[end].Definition.Opcode == UpdateRecord;
                end++;
            }

            if (!updatesRecord || end >= body.Count)
            {
                continue;
            }

            bool framed = body[end].Definition.Opcode == Deallocate
                && end + 1 < body.Count && body[end + 1].Definition.Opcode == Return;
            if (!framed && body[end].Definition.Opcode != Return)
            {
                continue;
            }

            string scratchKind = framed ? BeamOperandParameters.Y : BeamOperandParameters.X;
            var transfers = new List<(int Slot, CodeEdit Save, CodeEdit Use)>();
            for (int save = heap + 2; save + 1 < end; save++)
            {
                BeamInstruction producer = body[save - 1];
                if (producer.Definition.Opcode is not (PutTuple or PutList or UpdateRecord)
                    || producer.Operands[StackWriteOperand(producer)] != x0
                    || body[save] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { TypeIndex: null } slot]
                    } || result != x0 || slot.Kind != scratchKind || slot == x0)
                {
                    continue;
                }

                int use = save + 1;
                while (framed && use < end && body[use] is
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands:
                        [
                        { Kind: BeamOperandParameters.Y } carrier,
                            _,
                        { Kind: BeamOperandParameters.X, Value.IsZero: false }
                        ]
                    } && !SameProjectionRegister(carrier, slot))
                {
                    use++;
                }

                if (use == end || !ConsumesTerminalConstructionScratch(body[use], slot, x0)
                    || retiredRecord is { } profile && (!framed || save != heap + 2 || use != save + 1
                        || !RetiresOwnedRecord(
                            heap,
                            use,
                            end,
                            slot,
                            profile
                        ))
                    || !framed && (body[use].Definition.Opcode != PutList || body[use].Operands[0] != slot)
                    || !IsTerminalConstructionScratchDead(
                        body,
                        use + 1,
                        end,
                        slot
                    ))
                {
                    continue;
                }

                CodeEdit input;
                if (!framed)
                {
                    // The plain scratch is the first explicit operand of put_list.
                    int head = body[use].Offset + 1;
                    var cursor = new BeamCursor(code[head..]);
                    _ = BeamCompactReader.Read(cursor);
                    input = new CodeEdit(
                        head,
                        head + cursor.Position,
                        XRegisterTag,
                        0
                    );
                }
                else
                {
                    int operand = FirstStackOperandAtOrAfter(stack, body[use].Offset);
                    while (operand < stack.Count && stack[operand].Start < body[use + 1].Offset
                        && stack[operand].Slot != slot.Value)
                    {
                        operand++;
                    }

                    if (operand >= stack.Count || stack[operand].Start >= body[use + 1].Offset)
                    {
                        continue;
                    }

                    var (start, finish, _) = stack[operand];
                    input = new CodeEdit(
                        start,
                        finish,
                        XRegisterTag,
                        0
                    );
                }

                transfers.Add((
                    (int)slot.Value,
                    new CodeEdit(
                        body[save].Offset,
                        body[save + 1].Offset,
                        0,
                        0,
                        IsRemoval: true
                    ),
                    input
                ));
            }

            // A slot with repeated independent construction transfers is a
            // scratch slot. Each value is consumed before its next definition.
            var scratchSlots = transfers.GroupBy(transfer => transfer.Slot)
                .Where(group => retiredRecord is not null || group.Count() >= 2).Select(group => group.Key).ToHashSet();
            foreach (var transfer in transfers.Where(transfer => scratchSlots.Contains(transfer.Slot)))
            {
                edits.Add(transfer.Save);
                edits.Add(transfer.Use);
            }

            heap = end;
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        bool RetiresOwnedRecord(
            int heap,
            int use,
            int end,
            BeamOperand slot,
            (int Reverse, int Reuse, int ListType) profile
        ) =>
            heap > 0
            && body[heap - 1] is { Definition.Opcode: ExternalCall, Operands: [{ Value.IsOne: true }, var import] }
            && import.Value == profile.Reverse
            && body[heap].Operands[^1].Value.IsOne
            && body[heap + 1] is
            {
                Definition.Opcode: UpdateRecord,
                Operands: [var hint, _, var receiver, _, { Value: var count }, _, var replaced]
            } && hint.Value == profile.Reuse && count == 2 && SameProjectionRegister(slot, receiver)
            && SameProjectionRegister(x0, replaced) && replaced.TypeIndex == profile.ListType
            && body[use] is
            { Definition.Opcode: PutList, Operands: [_, { Kind: BeamOperandParameters.Nil }, _] }
            && !body.Skip(use + 1).Take(end - use - 1)
                .Any(instruction => instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)));
    }

    private static bool ConsumesTerminalConstructionScratch(BeamInstruction instruction, BeamOperand slot, BeamOperand x0)
    {
        if (instruction is { Definition.Opcode: PutList, Operands: [var first, var tail, var destination] })
        {
            return destination == x0 && SameProjectionRegister(first, slot)
                && !SameProjectionRegister(tail, slot) && !SameProjectionRegister(tail, x0);
        }

        return instruction is
        {
            Definition.Opcode: UpdateRecord,
            Operands: [_, _, var record, var updated, _, ..]
        }
            && updated == x0 && !SameProjectionRegister(record, slot)
            && instruction.Operands.Skip(5).Count(operand => SameProjectionRegister(operand, slot)) == 1;
    }

    private static bool IsTerminalConstructionScratchDead(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamOperand slot
    )
    {
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            int destination = StackWriteOperand(instruction);
            if (instruction.Operands.Where((_, position) => position != destination)
                .Any(operand => SameProjectionRegister(operand, slot)))
            {
                return false;
            }

            if (SameProjectionRegister(instruction.Operands[destination], slot))
            {
                return true;
            }
        }

        return true;
    }
}
