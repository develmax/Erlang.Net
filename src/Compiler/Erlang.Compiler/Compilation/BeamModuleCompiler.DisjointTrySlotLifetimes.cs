using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CompactDisjointTrySlotLifetimes(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(TryEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body = decoded.Body;
        var proved = new Dictionary<int, (int Count, int[] Slots)>();
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            if (entry < end && body[entry] is { Definition.Opcode: Label, Operands: [var label] }
                && Enumerable.Range(entry, end - entry).Count(index => body[index].Definition.Opcode == TryEnter) >= 2
                && !Enumerable.Range(entry, end - entry).Any(index =>
                    body[index].Definition.Opcode is CatchEnter or DebugLine or ExecutableLine)
                && (PlanBranchStackSlots(
                    body,
                    entry,
                    end,
                    allowSerialTryHandlers: true
                ) ?? (OnLoadFunction is null ? PlanBranchStackSlots(
                    body,
                    entry,
                    end,
                    allowSerialTryHandlers: true,
                    allowNestedTryHandlers: true
                ) : null)) is { Trim: null } layout)
            {
                proved.Add((int)label.Value, (layout.Count, layout.Slots));
            }

            entry = end + 1;
        }

        return proved.Count == 0 ? null : RewriteReusedStackSlots(
            code,
            body,
            decoded.StackOperands,
            useValueLiveness: false,
            closureEntries: null,
            atomicOnly: false,
            entriesToCompact: proved.Keys.ToHashSet(),
            provenBranchLayouts: proved
        );
    }
}
