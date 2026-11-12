using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool hasTupleProjections;

    private void WriteCodeInstructions(Stream output)
    {
        if (!hasTupleProjections)
        {
            instructions.WriteTo(output);

            return;
        }

        var code = instructions.GetBuffer().AsMemory(0, checked((int)instructions.Length));
        var header = new BeamCodeHeader(
            BeamWire.FormatVersion,
            highestOpcode,
            nextLabel,
            exports.Count + locals.Count,
            code
        );
        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            // The compiler can emit instructions outside the managed reader's operand subset.
            output.Write(code.Span);

            return;
        }

        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 0; index < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: GetTupleElement, Operands: [_, _, var temporary] }
                || temporary.TypeIndex is not null
                || temporary.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
            {
                continue;
            }

            int copy = FindProjectionCopy(body, index, temporary);
            if (copy < 0)
            {
                continue;
            }

            var destination = body[copy].Operands[1];
            if (!IsProjectionTemporaryDead(body, copy + 1, temporary)
                && !(temporary.Kind == BeamOperandParameters.Y
                    && (flow ??= new BeamProjectionFlow(body)).CanEliminate(index, copy + 1, temporary)))
            {
                continue;
            }

            int registerWidth = temporary.Value < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;
            int registerOffset = body[index + 1].Offset - registerWidth;
            output.Write(code.Span[copied..registerOffset]);
            BeamCompactWriter.Write(output, XRegisterTag, destination.Value);
            output.Write(code.Span[body[index + 1].Offset..body[copy].Offset]);
            copied = body[copy + 1].Offset;
        }

        output.Write(code.Span[copied..]);
    }

    private static int FindProjectionCopy(
        IReadOnlyList<BeamInstruction> body,
        int projection,
        BeamOperand temporary
    )
    {
        for (int index = projection + 1; index < body.Count; index++)
        {
            if (body[index].Definition.Opcode == Line)
            {
                continue;
            }

            if (body[index] is not { Definition.Opcode: Move, Operands: [var source, var destination] }
                || SameProjectionRegister(destination, temporary))
            {
                break;
            }

            if (!SameProjectionRegister(source, temporary))
            {
                continue;
            }

            if (source.TypeIndex is not null || destination is not { Kind: BeamOperandParameters.X, TypeIndex: null })
            {
                break;
            }

            for (int previous = projection + 1; previous < index; previous++)
            {
                if (body[previous].Operands.Any(operand => SameProjectionRegister(operand, destination)))
                {
                    return -1;
                }
            }

            return index;
        }

        return -1;
    }

    private static bool IsProjectionTemporaryDead(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand temporary
    )
    {
        for (int index = start; index < body.Count; index++)
        {
            var instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    continue;
                case Move:
                    if (instruction.Operands.Any(operand => SameProjectionRegister(operand, temporary)))
                    {
                        return false;
                    }

                    continue;
                case Deallocate:
                    return index + 1 < body.Count && body[index + 1].Definition.Opcode == Return
                        && (temporary.Kind == BeamOperandParameters.Y || !temporary.Value.IsZero);
                case Return:
                    return temporary.Kind == BeamOperandParameters.X && !temporary.Value.IsZero;
                case TailLocalCallWithFrame:
                case TailExternalCallWithFrame:
                case TailLocalCall:
                case TailExternalCall:
                case LocalCall:
                case ExternalCall:
                    // Both validators require initialized Y slots at calls.
                    return temporary.Kind == BeamOperandParameters.X
                        && temporary.Value >= instruction.Operands[0].Value;
                default:
                    return false;
            }
        }

        return false;
    }

    private static bool SameProjectionRegister(BeamOperand left, BeamOperand right) =>
        left.Kind == right.Kind && left.Value == right.Value;
}
