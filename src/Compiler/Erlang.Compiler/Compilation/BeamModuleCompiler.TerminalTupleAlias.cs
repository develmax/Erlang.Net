using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveTerminalTupleAlias(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 3 < body.Count; index++)
        {
            if (body[index - 1] is not { Definition.Opcode: PutTuple, Operands: [var built, ..] }
                || !PlainX(built, 0)
                || body[index] is not { Definition.Opcode: Move, Operands: [var source, var alias] }
                || source != built || !PlainX(alias, 1)
                || body[index + 1] is not { Definition.Opcode: PutTuple, Operands: [var destination, var length, ..] }
                || destination != source
                || !(body[index + 2].Definition.Opcode == Return
                    || body[index + 2].Definition.Opcode == Deallocate && body[index + 3].Definition.Opcode == Return))
            {
                continue;
            }

            var fields = body[index + 1].Operands.Skip(2).ToArray();
            if (!fields.Contains(alias) || fields.Any(field => field.TypeIndex is not null
                || field.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y or BeamOperandParameters.Integer
                    or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue)))
            {
                continue;
            }

            // The enclosing tuple reads all fields before overwriting X0, and the
            // return does not expose its temporary alias in X1.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(PutTuple);
            BeamCompactWriter.Write(output, XRegisterTag, destination.Value);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, length.Value);
            foreach (var field in fields)
            {
                switch (field.Kind)
                {
                    case BeamOperandParameters.X:
                        BeamCompactWriter.Write(output, XRegisterTag, field == alias ? source.Value : field.Value);
                        break;
                    case BeamOperandParameters.Y:
                        BeamCompactWriter.Write(output, YRegisterTag, field.Value);
                        break;
                    case BeamOperandParameters.Integer:
                        BeamCompactWriter.Write(output, BeamWire.IntegerTag, field.Value);
                        break;
                    case BeamOperandParameters.LiteralValue:
                        output.WriteByte(ExtendedLiteralOperand);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                        break;
                    default:
                        BeamCompactWriter.Write(output, AtomTag, field.Value);
                        break;
                }
            }

            copied = body[index + 2].Offset;
            index += 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool PlainX(BeamOperand operand, int index) =>
            operand.Kind == BeamOperandParameters.X && operand.TypeIndex is null && operand.Value == index;
    }
}
