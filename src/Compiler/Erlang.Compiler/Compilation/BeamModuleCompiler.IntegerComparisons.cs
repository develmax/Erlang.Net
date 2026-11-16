using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? NormalizeIntegerComparisons(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: IsLess, Operands: [var failure, var left, var right] })
            {
                continue;
            }

            bool constantFirst = left.Kind == BeamOperandParameters.Integer;
            BeamOperand constant = constantFirst ? left : right;
            BeamOperand register = constantFirst ? right : left;
            if (constant.Kind != BeamOperandParameters.Integer
                || register.TypeIndex is not int type || type >= typeBits.Count || typeBits[type].Bits != IntegerTypeBits)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(IsGreaterOrEqual);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            if (!constantFirst)
            {
                BeamCompactWriter.Write(output, BeamWire.IntegerTag, constant.Value - 1);
            }

            output.WriteByte(ExtendedTypedRegisterOperand);
            BeamCompactWriter.Write(output, register.Kind == BeamOperandParameters.X ? XRegisterTag : YRegisterTag, register.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            if (constantFirst)
            {
                BeamCompactWriter.Write(output, BeamWire.IntegerTag, constant.Value + 1);
            }

            copied = body[index + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
