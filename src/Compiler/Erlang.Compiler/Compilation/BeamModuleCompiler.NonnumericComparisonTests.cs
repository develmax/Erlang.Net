using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? NormalizeNonnumericComparisonTests(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (BeamInstruction instruction in body)
        {
            byte opcode = instruction.Definition.Opcode;
            if (opcode is not (NumericEqualTest or NumericNotEqualTest)
                || instruction.Operands is not [_, var left, var right])
            {
                continue;
            }

            bool includeNil = opcode == NumericNotEqualTest;
            if (!(IsRegister(left) && IsKnownLiteral(right, includeNil)
                || IsRegister(right) && IsKnownLiteral(left, includeNil)))
            {
                continue;
            }

            // A closed nonnumeric literal cannot require coercion of any
            // numeric descendant, so the exact comparison has the same result.
            output.Write(code.Span[copied..instruction.Offset]);
            byte exact = includeNil ? ExactNotEqualTest : ExactEqualTest;
            output.WriteByte(exact);
            highestOpcode = Math.Max(highestOpcode, exact);
            copied = instruction.Offset + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsRegister(BeamOperand operand) =>
            operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y;

        bool IsKnownLiteral(BeamOperand operand, bool includeNil) =>
            operand.Kind == BeamOperandParameters.Atom || includeNil && operand.Kind == BeamOperandParameters.Nil
            || operand.Kind == BeamOperandParameters.LiteralValue && nonnumericLiteralIndices.Contains((int)operand.Value);
    }
}
