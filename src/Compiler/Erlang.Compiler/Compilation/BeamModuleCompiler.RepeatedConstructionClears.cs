using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveRepeatedConstructionClears(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteRepeatedConstructionClears(code, body);
    }

    private static byte[]? RewriteRepeatedConstructionClears(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int allocation = 0; allocation + 4 < body.Count; allocation++)
        {
            int repeat = allocation + 3;
            if (body[allocation].Definition.Opcode != AllocateHeap
                || body[allocation + 1] is not { Definition.Opcode: InitYRegisters, Operands: var initialized }
                || body[allocation + 2] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null }, ..]
                }
                || body[repeat] is not { Definition.Opcode: InitYRegisters, Operands: var repeated }
                || repeated.Count < 2 || repeated.Skip(1).Any(slot => !initialized.Contains(slot))
                || body[repeat + 1] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [_, _, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                })
            {
                continue;
            }

            // These consecutive instructions remain in one allocation. The
            // tuple construction cannot change any already initialized slot.
            output.Write(code.Span[copied..body[repeat].Offset]);
            copied = body[repeat + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
