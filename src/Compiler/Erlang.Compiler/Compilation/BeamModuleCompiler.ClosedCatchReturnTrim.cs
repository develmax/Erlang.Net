using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimClosedCatchPayloadReturns(ReadOnlyMemory<byte> code, int labelCount) =>
        RewriteClosedCatchPayloadTrims(code, labelCount, terminalOnly: false);

    private byte[]? TrimClosedCatchPayloadExits(ReadOnlyMemory<byte> code, int labelCount) =>
        RewriteClosedCatchPayloadTrims(code, labelCount, terminalOnly: true);

    private byte[]? RewriteClosedCatchPayloadTrims(ReadOnlyMemory<byte> code, int labelCount, bool terminalOnly)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnd)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var body = decoded.Body;
        var edits = new SortedDictionary<int, (int Release, int Size)>();
        BeamProjectionFlow? flow = null;
        for (int info = 0; info + 12 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int end = info + 1;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int allocation = FindEntryFrameAllocation(
                body,
                info + 1,
                end,
                allowUnframedProjections: true
            );
            if (allocation < 0 || body[allocation].Operands[0].Value < 2
                || body[allocation].Operands[0].Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int frame = (int)body[allocation].Operands[0].Value;
            var payload = new BeamOperand(BeamOperandParameters.Y, frame - 1);
            int enter = -1;
            int closed = -1;
            bool valid = true;
            for (int index = allocation + 1; index < end; index++)
            {
                byte opcode = body[index].Definition.Opcode;
                if (opcode is Allocate or AllocateHeap or TrimFrame or TryEnter or TryEnd or TryCase or DebugLine or ExecutableLine)
                {
                    valid = false;
                    break;
                }

                if (opcode == CatchEnter)
                {
                    if (enter >= 0 || body[index].Operands[0] != payload)
                    {
                        valid = false;
                        break;
                    }

                    enter = index;
                }

                if (opcode == CatchEnd)
                {
                    if (enter < 0 || closed >= 0 || body[index].Operands[0] != payload
                        || body[index - 1] is not { Definition.Opcode: Label, Operands: [var landing] }
                        || body[enter].Operands[1].Value != landing.Value)
                    {
                        valid = false;
                        break;
                    }

                    closed = index;
                }
            }

            if (!valid || closed < 0 || closed + 6 >= end)
            {
                continue;
            }

            if (terminalOnly)
            {
                var flowAfterCatch = flow ??= new BeamProjectionFlow(body);
                int catchLabel = (int)body[enter].Operands[1].Value;
                if (body[closed - 2].Definition.Opcode is (LocalCall or ExternalCall)
                    && flowAfterCatch.TryGetExclusiveForwardTarget(
                        enter,
                        closed - 2,
                        catchLabel,
                        out int landing
                    ) && landing == closed - 1)
                {
                    ReadSelectedExitArms(
                        closed,
                        end,
                        frame,
                        payload,
                        flowAfterCatch
                    );
                }

                info = end - 1;
                continue;
            }

            if (body[closed + 1] is not
                {
                    Definition.Opcode: IsTuple, Operands: [var failure, var checkedTuple]
                } || checkedTuple != x0
                || body[closed + 2] is not
                {
                    Definition.Opcode: TestArity, Operands: [var wrongArity, var checkedAgain, var tupleArity]
                } || wrongArity != failure || checkedAgain != x0 || tupleArity.Value != 2
                || body[closed + 3] is not
                {
                    Definition.Opcode: GetTupleElement, Operands: [var tuple, { Value.IsZero: true }, var selector]
                } || tuple != x0
                || body[closed + 4] is not
                {
                    Definition.Opcode: GetTupleElement, Operands: [var sameTuple, { Value.IsOne: true }, var field]
                } || sameTuple != x0 || field != payload
                || body[closed + 5] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var discriminant, var fallback, var alternatives, { Kind: BeamOperandParameters.Atom }, var first,
                    { Kind: BeamOperandParameters.Atom }, var second]
                } || discriminant != selector || fallback != failure || alternatives.Value != 4)
            {
                continue;
            }

            var ownership = flow ??= new BeamProjectionFlow(body);
            foreach (var branch in new[] { first, second })
            {
                if (!ownership.TryGetSingleForwardTarget(closed + 5, (int)branch.Value, out int target))
                {
                    continue;
                }

                int release = ReadReturningArm(
                    target + 1,
                    end,
                    frame,
                    payload
                );
                if (release >= 0)
                {
                    edits.Add(target + 1, (release, frame));
                }
            }

            info = end - 1;
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (var (site, (release, frame)) in edits)
        {
            int start = body[site].Offset;
            int finish = body[release].Offset;
            output.Write(code.Span[copied..start]);
            output.WriteByte(TrimFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame - 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = start;
            foreach (var slot in decoded.StackOperands.Where(slot => slot.Start >= start && slot.Start < finish))
            {
                output.Write(code.Span[copied..slot.Start]);
                BeamCompactWriter.Write(output, YRegisterTag, 0);
                copied = slot.End;
            }

            int operandCount = LinearFrameSizeOperand(body[release].Definition.Opcode);
            var cursor = new BeamCursor(code[(finish + 1)..]);
            for (int operand = 0; operand < operandCount; operand++)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            output.Write(code.Span[copied..(finish + 1 + cursor.Position)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = body[release + 1].Offset;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void ReadSelectedExitArms(
            int closed,
            int end,
            int size,
            BeamOperand payload,
            BeamProjectionFlow ownership
        )
        {
            if (body[closed + 1] is not
                {
                    Definition.Opcode: IsTuple, Operands: [{ Kind: BeamOperandParameters.Label } failed, var tuple]
                } || tuple != x0
                || body[closed + 2] is not
                {
                    Definition.Opcode: SelectTupleArity, Operands: [var tested, var invalid, _, _, _, _, _, ..]
                } aritySelector || !SameProjectionRegister(tested, tuple) || invalid != failed)
            {
                return;
            }

            int exitImport = ReadExitImport();
            if (exitImport < 0)
            {
                return;
            }

            for (int option = 3; option + 1 < aritySelector.Operands.Count; option += 2)
            {
                if (aritySelector.Operands[option].Value != 2
                    || aritySelector.Operands[option + 1] is not { Kind: BeamOperandParameters.Label } arityTarget
                    || !ownership.TryGetSingleForwardTarget(closed + 2, (int)arityTarget.Value, out int group)
                    || group + 2 >= end
                    || body[group + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var original, { Value.IsZero: true },
                        { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true } tag]
                    }
                    || original != tuple || body[group + 2] is not
                    {
                        Definition.Opcode: SelectValue, Operands: [var compared, var rejection, _, _, _, ..]
                    } tags || compared != tag || rejection != failed)
                {
                    continue;
                }

                for (int key = 3; key + 1 < tags.Operands.Count; key += 2)
                {
                    if (tags.Operands[key].Kind != BeamOperandParameters.Atom
                        || tags.Operands[key + 1] is not { Kind: BeamOperandParameters.Label } selected
                        || !ownership.TryGetSingleForwardTarget(group + 2, (int)selected.Value, out int branch)
                        || ReadExitArm(
                            branch + 1,
                            end,
                            size,
                            payload,
                            exitImport
                        ) is not { } arm)
                    {
                        continue;
                    }

                    edits.Add(arm.Site, (arm.Release, size));
                }
            }
        }

        (int Site, int Release)? ReadExitArm(
            int start,
            int end,
            int size,
            BeamOperand payload,
            int exitImport
        )
        {
            if (start + 5 >= end || body[start] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Value.IsOne: true }, var reason]
                } || source != x0 || reason != payload
                || body[start + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } argument, var incoming]
                } || argument.Value < 0 || argument.Value >= payload.Value || incoming != x0)
            {
                return null;
            }

            int firstCall = SkipLines(start + 2, end);
            if (firstCall >= end || body[firstCall] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] })
            {
                return null;
            }

            int calls = 1;
            for (int index = firstCall + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction is { Definition.Opcode: Move, Operands: [var kept, var output] }
                    && kept == payload && output == x0)
                {
                    int release = SkipLines(index + 1, end);

                    return calls == 2 && release < end && body[release] is
                    {
                        Definition.Opcode: TailExternalCallWithFrame,
                        Operands: [{ Value.IsOne: true }, var import, var frame]
                    } && import.Value == exitImport && frame.Value == size
                        ? (start + 2, release) : null;
                }

                if (instruction.Definition.Opcode is not
                    (Line or Move or TestHeap or PutList or PutTuple or LocalCall or ExternalCall)
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    return null;
                }

                if (instruction.Definition.Opcode is LocalCall or ExternalCall)
                {
                    calls++;
                }
            }

            return null;
        }

        int SkipLines(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        int ReadExitImport()
        {
            if (!atoms.TryGetValue(ErlangModuleNames.Module, out int module)
                || !atoms.TryGetValue(AutoImportNames.Exit, out int function)
                || !imports.TryGetValue((module, function, 1), out int index))
            {
                return -1;
            }

            return emittedImports?[index] ?? index;
        }

        int ReadReturningArm(
            int start,
            int end,
            int size,
            BeamOperand payload
        )
        {
            if (start + 4 >= end || body[start] is not { Definition.Opcode: Move, Operands: [var from, var to] }
                || from != payload || to != x0)
            {
                return -1;
            }

            int call = start + 1;
            while (call < end && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 2 >= end || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] })
            {
                return -1;
            }

            bool keepsPayload = false;
            for (int index = call + 1; index + 1 < end; index++)
            {
                var instruction = body[index];
                if (instruction.Definition.Opcode == Deallocate)
                {
                    return keepsPayload && instruction.Operands[0].Value == size
                        && body[index + 1].Definition.Opcode == Return ? index : -1;
                }

                if (instruction.Definition.Opcode is not (Line or TestHeap or PutTuple or PutList))
                {
                    return -1;
                }

                if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y && operand != payload))
                    {
                        return -1;
                    }

                    keepsPayload = true;
                }
            }

            return -1;
        }
    }
}
