using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveOverwrittenHandlerSlotCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(CatchEnter) && !code.Span.Contains(TryEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null },
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } destination]
                }
                || body[index + 1] is not
                {
                    Definition.Opcode: CatchEnter or TryEnter,
                    Operands: [var handler, { Kind: BeamOperandParameters.Label, Value.Sign: > 0 }]
                } || handler != destination)
            {
                continue;
            }

            // Handler installation replaces the term in its slot. With no
            // operation or label between the writes, the term is never read.
            output.Write(code.Span[copied..body[index].Offset]);
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
