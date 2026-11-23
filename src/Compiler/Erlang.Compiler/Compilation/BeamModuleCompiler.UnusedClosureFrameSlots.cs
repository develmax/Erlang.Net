using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveUnusedClosureFrameSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || closures.Count == 0
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        var entries = closures.Select(closure => MapClosureEntryLabel(closure.Entry)).ToHashSet();
        var unused = ReadUnusedClosureFrameEntries(body, entries);
        if (unused.Count == 0)
        {
            return null;
        }

        return RewriteDisjointFrames(
            code,
            body,
            operands,
            atomicOnly: false,
            entriesToCompact: unused
        );
    }

    private static HashSet<int> ReadUnusedClosureFrameEntries(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlySet<int> entries
    )
    {
        var selected = new HashSet<int>();
        for (int info = 0; info + 2 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo
                || body[info + 1] is not { Definition.Opcode: Label, Operands: [var label] }
                || !entries.Contains((int)label.Value))
            {
                continue;
            }

            int start = info + 1;
            int end = start;
            while (end < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            // Late closure forwarding can leave initialized slots with no
            // remaining reads. Reuse the existing frame ownership check and
            // preserve the lexical closure entry and caller continuation.
            if (ReadDisjointFrameOwnership(body, start, end) is { } owned
                && owned.Uses.Count == 1
                && owned.Uses.Values.Single().Any(used => !used))
            {
                selected.Add((int)label.Value);
            }
        }

        return selected;
    }
}
