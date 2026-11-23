using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CompactDeadSlotsThroughOwnedTrim(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteDisjointFrameLayouts(
            code,
            decoded.Body,
            decoded.StackOperands,
            atomicOnly: false,
            completeTrimsOnly: false,
            entriesToCompact: null,
            reuseOccupiedEntrySlots: false,
            onlyAllocation: null,
            compactDeadTrimSlots: true
        );
    }

    private static ((int Size, int[] Slots)?[] Layouts, IReadOnlyDictionary<int, int> Kept)? PlanDeadTrimSlotLayouts(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        int allocation = -1;
        var trims = new List<int>();
        for (int index = start; index < end; index++)
        {
            switch (body[index].Definition.Opcode)
            {
                case Allocate when allocation < 0:
                    allocation = index;
                    break;

                case TrimFrame when allocation >= 0:
                    trims.Add(index);
                    break;

                case Allocate or AllocateHeap or TrimFrame:
                    return null;
            }
        }

        if (allocation < 0 || trims.Count == 0
            || ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } frame)
        {
            return null;
        }

        var origins = new Dictionary<int, int> { [allocation] = 0 };
        int width = frame.Uses[allocation].Length;
        foreach (int trim in trims)
        {
            if (!origins.TryGetValue(frame.Owners[trim - start], out int origin)
                || body[trim] is not { Operands: [{ Value: var removed }, { Value: var kept }] }
                || removed.Sign <= 0 || kept.Sign <= 0 || removed + origin + kept != width)
            {
                return null;
            }

            origins.Add(trim, origin + (int)removed);
        }

        // Translate each frame epoch back to original stack addresses.
        // A raw trim can discard only a prefix, so keep every slot used by
        // any epoch and compact the inherited suffix in the same order.
        var used = new bool[width];
        foreach (var (owner, origin) in origins)
        {
            bool[] usage = frame.Uses[owner];
            for (int slot = 0; slot < usage.Length; slot++)
            {
                used[origin + slot] |= usage[slot];
            }
        }

        var prefix = new int[width + 1];
        for (int slot = 0; slot < width; slot++)
        {
            prefix[slot + 1] = prefix[slot] + (used[slot] ? 1 : 0);
        }

        int size = prefix[width];
        if (size == width)
        {
            return null;
        }

        var epochs = new Dictionary<int, (int Size, int[] Slots)>();
        var keep = new Dictionary<int, int>();
        foreach (var (owner, origin) in origins)
        {
            int kept = size - prefix[origin];
            if (kept == 0)
            {
                return null;
            }

            var slots = new int[width - origin];
            for (int slot = 0; slot < slots.Length; slot++)
            {
                int absolute = origin + slot;
                slots[slot] = used[absolute] ? prefix[absolute] - prefix[origin] : -1;
            }

            epochs.Add(owner, (kept, slots));
            if (owner != allocation)
            {
                keep.Add(owner, kept);
            }
        }

        var layouts = new (int Size, int[] Slots)?[end - start];
        for (int index = allocation; index < end; index++)
        {
            int owner = index == allocation ? allocation : frame.Owners[index - start];
            if (epochs.TryGetValue(owner, out var layout))
            {
                layouts[index - start] = layout;
            }
        }

        return (layouts, keep);
    }
}
