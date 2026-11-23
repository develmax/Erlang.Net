using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateDominatedAtomComparisons(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(IsAtom)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index < body.Count; index++)
        {
            if (body[index - 1] is not
                {
                    Definition.Opcode: IsAtom,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Label } rejected,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } tested
                    ]
                }
                || rejected.Value <= 0 || body[index] is not
                {
                    Definition.Opcode: ExactNotEqualTest,
                    Operands: [var failure, var input, { Kind: BeamOperandParameters.Atom }]
                } comparison || failure != rejected || input != tested)
            {
                continue;
            }

            // The comparison has no intervening label or definition. Its only
            // incoming edge is successful fall-through from the atom test.
            copied = AnnotateRegisterInput(
                code,
                output,
                copied,
                comparison,
                1,
                AtomTypeBits
            );
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
