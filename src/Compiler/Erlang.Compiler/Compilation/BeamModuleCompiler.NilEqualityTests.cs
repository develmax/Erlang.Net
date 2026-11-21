using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? NormalizeNilEqualityTests(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        bool normalizeNumeric = SsaTypeInferenceEnabled && !ModuleOptimizationDisabled;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode != ExactEqualTest
                && (!normalizeNumeric || instruction.Definition.Opcode != NumericEqualTest)
                || instruction.Operands is not [var failure, var left, var right])
            {
                continue;
            }

            bool leftNil = left.Kind == BeamOperandParameters.Nil;
            BeamOperand register = leftNil ? right : left;
            if (!leftNil && right.Kind != BeamOperandParameters.Nil
                || register.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
            {
                continue;
            }

            // Nil contains no numbers, so loose equality has the same success
            // edge as exact equality. OTP beam_z encodes the exact test as is_nil.
            output.Write(code.Span[copied..instruction.Offset]);
            output.WriteByte(IsNil);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            if (register.TypeIndex.HasValue)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, register);
            if (register.TypeIndex is int type)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }

            copied = body[index + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        highestOpcode = Math.Max(highestOpcode, IsNil);

        return output.ToArray();
    }
}
