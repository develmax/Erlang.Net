using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardJoinedLiteralCallArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamOperand x2 = new(BeamOperandParameters.X, 2);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (!HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            for (int save = entry + 4; save + 14 < end; save++)
            {
                if (body[save] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                    } || result != x0 || body[save - 1].Definition.Opcode != Label
                    || !LoadsLiteral(body[save - 2])
                    || body[save + 1] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var retry, { Kind: BeamOperandParameters.Y, TypeIndex: null } choice,
                        { Kind: BeamOperandParameters.Atom }]
                    } || choice == slot || !IsMove(body[save + 2], slot, x1)
                    || !ReadsJoinedStackArgument(body[save + 3], 2, out var argument)
                    || !ReadsJoinedStackArgument(body[save + 4], 0, out var receiver))
                {
                    continue;
                }

                int first = SkipLines(save + 5);
                int secondArm = first + 2;
                if (!IsCall(first, 3) || secondArm + 3 >= end
                    || body[first + 1] is not { Definition.Opcode: Jump, Operands: [var joined] }
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                        save + 1,
                        (int)retry.Value,
                        out int retryAt
                    ) || retryAt != secondArm
                    || !IsMove(body[secondArm + 1], slot, x1)
                    || !IsMove(body[secondArm + 2], argument, x2)
                    || !IsMove(body[secondArm + 3], receiver, x0))
                {
                    continue;
                }

                int second = SkipLines(secondArm + 4);
                if (!IsCall(second, 3)
                    || !flow.TryGetExclusiveForwardTarget(
                        first + 1,
                        second,
                        (int)joined.Value,
                        out int join
                    ) || join != second + 1
                    || !flow.HasJoinedLiteralResult(save - 1, x0)
                    || flow.PlanSavedCallResult(
                        save,
                        end,
                        slot,
                        x0,
                        null
                    ) is not { Count: 2 } uses
                    || !uses.Contains(save + 2) || !uses.Contains(secondArm + 1))
                {
                    continue;
                }

                BeamOperand[] dead = [slot, choice, argument];
                if (dead.Distinct().Count() != dead.Length || dead.Contains(receiver)
                    || ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                    || !ownership.Uses.TryGetValue(ownership.Owners[save - entry], out bool[]? frame)
                    || frame.Length != dead.Length + 1 || receiver.Value >= frame.Length
                    || dead.Any(operand => operand.Value >= frame.Length
                        || !flow.IsInitialized(save, operand) || !UnusedAfterJoin(operand)))
                {
                    continue;
                }

                output.Write(code.Span[copied..body[save].Offset]);
                copied = body[save + 1].Offset;
                WriteArm(save + 2);
                WriteArm(secondArm + 1);

                // Only the success continuation reloads the receiver for the final
                // effect. Once it is in X0, that continuation owns no stack roots.
                for (int call = join + 2; call + 3 < end; call++)
                {
                    if (!IsCall(call, 2) || !IsMove(body[call - 1], receiver, x0)
                        || body[call - 2] is not
                        {
                            Definition.Opcode: GcBifTwo,
                            Operands: [{ Value.IsZero: true }, _, _, var left, var right, var destination]
                        } || left != x0 || !IsNonRegisterValue(right) || destination != x1
                        || !LoadsLiteral(body[call + 1])
                        || body[call + 2] is not { Definition.Opcode: Deallocate, Operands: [var size] }
                        || size.Value != frame.Length || body[call + 3].Definition.Opcode != Return
                        || ownership.Owners[call - entry] != ownership.Owners[save - entry])
                    {
                        continue;
                    }

                    output.Write(code.Span[copied..body[call].Offset]);
                    output.WriteByte(TrimFrame);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Length);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    output.Write(code.Span[body[call].Offset..body[call + 2].Offset]);
                    output.WriteByte(Deallocate);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    copied = body[call + 3].Offset;
                    break;
                }

                break;

                bool UnusedAfterJoin(BeamOperand operand)
                {
                    for (int index = join + 1; index < end; index++)
                    {
                        if (body[index].Definition.Opcode is TrimFrame or Allocate or AllocateHeap
                            || body[index].Operands.Any(value => SameProjectionRegister(value, operand)))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                void WriteArm(int load)
                {
                    output.Write(code.Span[copied..body[load].Offset]);
                    output.WriteByte(Move);
                    WriteRegister(output, x0);
                    WriteRegister(output, x1);
                    output.Write(code.Span[body[load + 1].Offset..body[load + 2].Offset]);
                    WriteFrameInitialization(output, dead.OrderBy(value => value.Value).Select(value => (int)value.Value).ToArray());
                    copied = body[load + 2].Offset;
                }
            }

            int SkipLines(int index)
            {
                while (index < end && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }

            bool IsCall(int index, int arity) => index < end && body[index] is
            { Definition.Opcode: LocalCall or ExternalCall, Operands: [var amount, _] } && amount.Value == arity;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool LoadsLiteral(BeamInstruction instruction) => instruction is
        { Definition.Opcode: Move, Operands: [var literal, var destination] }
            && destination == x0 && IsNonRegisterValue(literal);
    }

    private static bool ReadsJoinedStackArgument(BeamInstruction instruction, int argument, out BeamOperand slot)
    {
        if (instruction is
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            } && destination.Value == argument)
        {
            slot = source;

            return true;
        }

        slot = null!;

        return false;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasJoinedLiteralResult(int join, BeamOperand result)
        {
            if (predecessors[join].Count != 2)
            {
                return false;
            }

            foreach (int edge in predecessors[join])
            {
                int producer = edge;
                if (body[edge].Definition.Opcode == Jump)
                {
                    if (predecessors[edge] is not [var previous] || previous + 1 != edge)
                    {
                        return false;
                    }

                    producer = previous;
                }

                if (producer >= join || body[producer] is not
                    { Definition.Opcode: Move, Operands: [var literal, var destination] }
                    || !IsNonRegisterValue(literal) || destination != result)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
