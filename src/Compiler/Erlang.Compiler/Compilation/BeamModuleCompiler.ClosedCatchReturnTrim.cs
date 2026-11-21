using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimClosedCatchPayloadReturns(ReadOnlyMemory<byte> code, int labelCount)
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

            if (!valid || closed < 0 || closed + 6 >= end
                || body[closed + 1] is not
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

            output.Write(code.Span[copied..finish]);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = body[release + 1].Offset;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

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
