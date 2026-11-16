using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldStackAndArgumentSwaps(ReadOnlyMemory<byte> code)
    {
        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(
                new BeamCodeHeader(
                    BeamWire.FormatVersion,
                    highestOpcode,
                    nextLabel,
                    exports.Count + locals.Count,
                    code
                ),
                atoms.Count + 1,
                literalBytes.Count
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: Move, Operands: [var from, var to] }
                && IsSwapRegister(from) && IsSwapRegister(to) && from != to
                && body[index + 1] is { Definition.Opcode: Swap, Operands: [var left, var right] })
            {
                BeamOperand? other = to == left ? right : to == right ? left : null;
                if (other is not null && IsSwapRegister(other) && other != from && other != to)
                {
                    output.Write(code.Span[copied..body[index].Offset]);
                    output.WriteByte(Move);
                    WriteRegister(output, other);
                    WriteRegister(output, to);
                    output.WriteByte(Move);
                    WriteRegister(output, from);
                    WriteRegister(output, other);
                    copied = body[index + 2].Offset;
                    index++;
                    continue;
                }
            }

            int argumentSwap = FindIndependentArgumentSwap(body, index);
            if (argumentSwap >= 0)
            {
                output.Write(code.Span[copied..body[index].Offset]);
                output.Write(code.Span[body[argumentSwap].Offset..body[argumentSwap + 1].Offset]);
                output.Write(code.Span[body[index].Offset..body[argumentSwap].Offset]);
                copied = body[argumentSwap + 1].Offset;
                index = argumentSwap;
                continue;
            }

            if (body[index] is not { Definition.Opcode: Move, Operands: [var first, var temporary] }
                || !IsSwapRegister(first) || temporary is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                || SameProjectionRegister(first, temporary)
                || body[index + 1] is not { Definition.Opcode: Move, Operands: [var second, var destination] }
                || !IsSwapRegister(second) || destination != first
                || SameProjectionRegister(second, first) || SameProjectionRegister(second, temporary))
            {
                continue;
            }

            int closingMove = FindSwapClosingMove(
                body,
                index + 2,
                temporary,
                second
            );
            if (closingMove < 0)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(Swap);
            WriteRegister(output, first);
            WriteRegister(output, second);
            output.Write(code.Span[body[index + 2].Offset..body[closingMove].Offset]);
            copied = body[closingMove + 1].Offset;
            index = closingMove;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int FindIndependentArgumentSwap(IReadOnlyList<BeamInstruction> body, int start)
    {
        int index = start;
        while (index < body.Count && body[index] is
            { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X } destination] }
            && !destination.Value.IsZero)
        {
            index++;
        }

        if (index == start || index >= body.Count || body[index] is not
            {
                Definition.Opcode: Swap, Operands: [
                { Kind: BeamOperandParameters.Y } slot, { Kind: BeamOperandParameters.X } argument]
            }
            || !argument.Value.IsZero)
        {
            return -1;
        }

        int call = index + 1;
        while (call < body.Count && body[call].Definition.Opcode == Line)
        {
            call++;
        }

        if (call == body.Count || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call].Operands[0].Value.IsZero)
        {
            return -1;
        }

        for (int move = start; move < index; move++)
        {
            BeamOperand source = body[move].Operands[0];
            if (SameProjectionRegister(source, slot) || SameProjectionRegister(source, argument))
            {
                return -1;
            }
        }

        return index;
    }

    private static int FindSwapClosingMove(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand temporary,
        BeamOperand second
    )
    {
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
                && source == temporary && destination == second)
            {
                return IsDeadSwapTemporary(body, index + 1, temporary) ? index : -1;
            }

            if (instruction.Definition.Opcode is not (Move or InitYRegisters)
                || instruction.Operands.Any(operand =>
                    SameProjectionRegister(operand, temporary) || SameProjectionRegister(operand, second)))
            {
                break;
            }
        }

        return -1;
    }

    private static bool IsDeadSwapTemporary(IReadOnlyList<BeamInstruction> body, int start, BeamOperand temporary)
    {
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    continue;
                case LocalCall:
                case ExternalCall:
                    return instruction.Operands[0].Value <= temporary.Value;
                case Move:
                    if (SameProjectionRegister(instruction.Operands[0], temporary))
                    {
                        return false;
                    }

                    if (SameProjectionRegister(instruction.Operands[1], temporary))
                    {
                        return true;
                    }

                    continue;
                default:
                    return false;
            }
        }

        return false;
    }

    private static void WriteRegister(Stream output, BeamOperand operand) =>
        BeamCompactWriter.Write(output, operand.Kind == BeamOperandParameters.X ? XRegisterTag : YRegisterTag, operand.Value);

    private static bool IsSwapRegister(BeamOperand operand) =>
        operand is { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null };
}
