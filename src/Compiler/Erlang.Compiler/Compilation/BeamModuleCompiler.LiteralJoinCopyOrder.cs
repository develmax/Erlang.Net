using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderLiteralJoinCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Jump)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int join = 3; join < body.Count; join++)
        {
            if (body[join].Definition.Opcode != Label
                || body[join - 2] is not
                { Definition.Opcode: Move, Operands: [var left, { Kind: BeamOperandParameters.Y, TypeIndex: null } first] }
                || body[join - 1] is not
                { Definition.Opcode: Move, Operands: [var right, { Kind: BeamOperandParameters.Y, TypeIndex: null } second] }
                || first == second || !IsJoinLiteral(left) || !IsJoinLiteral(right)
                || !(flow ??= new BeamProjectionFlow(body)).HasReversedRegisterJoinCopies(
                    join,
                    first,
                    second
                ))
            {
                continue;
            }

            // The two phi inputs define the same slots. The sibling copies
            // cannot form a dependency chain, since they read only X registers.
            // Literal copies can use the same slot order without changing a root.
            int begin = body[join - 2].Offset;
            int middle = body[join - 1].Offset;
            int end = body[join].Offset;
            output.Write(code.Span[copied..begin]);
            output.Write(code.Span[middle..end]);
            output.Write(code.Span[begin..middle]);
            copied = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsJoinLiteral(BeamOperand operand) => operand.Kind is BeamOperandParameters.Atom
            or BeamOperandParameters.Integer or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasReversedRegisterJoinCopies(int join, BeamOperand first, BeamOperand second)
        {
            if (!IsProjectionFlowTest(body[join - 3].Definition.Opcode)
                || predecessors[join].Count != 2)
            {
                return false;
            }

            int other = predecessors[join][0] == join - 1 ? predecessors[join][1] : predecessors[join][0];

            return other >= 2 && other < join - 3 && predecessors[join].Contains(join - 1)
                && body[other].Definition.Opcode == Jump
                && IsRegisterCopy(body[other - 2], second)
                && IsRegisterCopy(body[other - 1], first);

            static bool IsRegisterCopy(BeamInstruction instruction, BeamOperand slot) =>
                instruction is
                { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null }, var destination] }
                && destination == slot;
        }
    }
}
