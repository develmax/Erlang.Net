using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ExchangeCallResultWithDeadArgument(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        int encoded = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            int frame = FindLinearFrameSuffix(body, entry, end);
            if (PlanLinearStackSlots(
                body,
                frame,
                end,
                includeUnchanged: true
            ) is null)
            {
                continue;
            }

            for (int store = frame + 2; store + 4 < end; store++)
            {
                if (body[store - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[store] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || returned != result
                    || body[store + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var other, { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null }]
                    }
                    || body[store + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } argument, var destination]
                    } || destination != result || saved == argument || SameProjectionRegister(other, argument)
                    || SameProjectionRegister(other, saved) || SameProjectionRegister(other, result))
                {
                    continue;
                }

                int call = store + 3;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call == end || body[call] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] }
                    || arity != 2)
                {
                    continue;
                }

                int limit = call + 1;
                bool used = false;
                while (limit < end)
                {
                    BeamInstruction instruction = body[limit];
                    int written = StackAllocationDestination(instruction);
                    if (instruction.Operands.Any(operand => SameProjectionRegister(operand, argument)))
                    {
                        // An overwrite ends the displaced argument's lifetime. A read
                        // would observe the new value and forbids sharing the slot.
                        bool replacesArgument = instruction.Definition.Opcode == InitYRegisters
                            || written >= 0 && SameProjectionRegister(instruction.Operands[written], argument)
                            && !instruction.Operands.Where((_, index) => index != written)
                                .Any(operand => SameProjectionRegister(operand, argument));
                        if (!replacesArgument)
                        {
                            used = false;
                        }

                        break;
                    }

                    if (written >= 0 && SameProjectionRegister(instruction.Operands[written], saved)
                        || instruction.Definition.Opcode == InitYRegisters && instruction.Operands.Skip(1).Contains(saved))
                    {
                        break;
                    }

                    used |= instruction.Operands.Any(operand => SameProjectionRegister(operand, saved));
                    limit++;
                }

                if (!used || limit < end && body.Skip(limit).Take(end - limit).Any(instruction =>
                    instruction.Operands.Any(operand => SameProjectionRegister(operand, saved))))
                {
                    continue;
                }

                // The argument becomes dead at the call. The new result may
                // occupy its slot while X0 supplies the old value to the callee.
                output.Write(code.Span[copied..body[store].Offset]);
                output.WriteByte(Swap);
                WriteRegister(output, argument);
                WriteRegister(output, result);
                output.Write(code.Span[body[store + 1].Offset..body[store + 2].Offset]);
                copied = body[store + 3].Offset;
                while (encoded < stackOperands.Count && stackOperands[encoded].Start < copied)
                {
                    encoded++;
                }

                int stop = body[limit].Offset;
                while (encoded < stackOperands.Count && stackOperands[encoded].Start < stop)
                {
                    var (from, to, slot) = stackOperands[encoded++];
                    if (slot == saved.Value)
                    {
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, (int)argument.Value);
                        copied = to;
                    }
                }

                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
