using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveDeadTupleProjections(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && code.Span.Contains(GetTupleElement)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } decoded
            ? RewriteDeadTupleProjections(code, decoded.Body)
            : null;

    private static byte[]? RewriteDeadTupleProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var tuple, _, { Kind: BeamOperandParameters.X, TypeIndex: null } result]
                } || SameProjectionRegister(tuple, result)
                || !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(index, result))
            {
                continue;
            }

            // The checked field extraction has no effect. Its destination also
            // lies outside every implicit call or GC use before the next definition.
            edits.Add(new(
                body[index].Offset,
                body[index + 1].Offset,
                0,
                0,
                IsRemoval: true
            ));
        }

        return edits.Count == 0 ? null : ApplyDuplicatedFrameReadEdits(code, edits);
    }
}
