namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    private readonly HashSet<long> oldBlocks508 = new();
    private long highWaterIdentity508;
    private ushort minorCollections508;
    private long completedCollections508;
    private long reclaimedPayloadWords508;
    private long reclaimedAllocationWords567;

    internal ProcessTermGenerationSnapshot508 GenerationSnapshot508
    {
        get
        {
            lock (Gate)
            {
                long nursery = 0, survivor = 0, old = 0;
                foreach (var entry in blocks)
                {
                    if (oldBlocks508.Contains(entry.Key))
                        old += entry.Value.Count;
                    else if (entry.Key <= highWaterIdentity508)
                        survivor += entry.Value.Count;
                    else
                        nursery += entry.Value.Count;
                }

                return new(
                    nursery,
                    survivor,
                    old,
                    completedCollections508,
                    reclaimedPayloadWords508
                );
            }
        }
    }

    // Nonmoving owned-slab collector. It scans every registered root and old
    // block child, so no write barrier or unregistered CLR-stack root is assumed.
    // Only explicit safe points may call it; Allocate cannot collect while a
    // partially imported graph has unpublished intermediate payload blocks.
    internal ProcessTermCollection508 CollectGeneration508(bool requestedMajor, ushort fullSweepAfter)
    {
        lock (Gate)
        {
            bool major = requestedMajor || minorCollections508 >= fullSweepAfter;
            var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
            var marked = new HashSet<long>();
            var pending = new Stack<Term>(frames.SelectMany(frame => frame.Roots));
            if (!major)
            {
                // Even an unreachable old object is retained by a minor pass.
                // Keep any younger children valid until a full collection can
                // retire that whole unreachable old graph.
                foreach (long id in oldBlocks508)
                {
                    ProcessTermSequence506 block = blocks[id];
                    for (int index = 0; index < block.Count; index++)
                        pending.Push(block.Slab.Cells[block.Offset + index]!);
                }
            }
            while (pending.TryPop(out Term? term))
            {
                if (!seen.Add(term))
                    continue;
                if (Storage(term) is { } block)
                {
                    _ = IsOwnedLocal(term);
                    marked.Add(block.Identity);
                }
                foreach (Term child in NativeMapOwnedRoots510.Children(term))
                    pending.Push(child);
            }
            MarkResourceBinaryNodes577(seen, major, marked);
            ProcessNativeCollection524? nativeCollection = PrepareNativeCollection524(seen, major, marked);
            MagicReferenceSweep576? magicCollection = PrepareMagicCollection576(marked, major);
            long[] dead = blocks.Keys.Where(id => !marked.Contains(id)
                && (major || !oldBlocks508.Contains(id))).ToArray();
            long reclaimed = dead.Sum(id => (long)blocks[id].Count);
            long reclaimedAllocation = dead.Sum(id => (long)blocks[id].AllocationWords526);
            long completed = checked(completedCollections508 + 1);
            long totalReclaimed = checked(reclaimedPayloadWords508 + reclaimed);
            long totalReclaimedAllocation = checked(reclaimedAllocationWords567 + reclaimedAllocation);
            ushort nextMinor = major ? (ushort)0 : checked((ushort)(minorCollections508 + 1));
            // All traversal/validation and checked scalar calculations complete
            // before cells are released or collection state advances.
            nativeCollection?.Commit524();
            magicCollection?.Commit576();
            foreach (long id in dead)
            {
                ProcessTermSequence506 block = blocks[id];
                ReleaseAllocation519(block.Slab, block.AllocationOffset526, block.AllocationWords526);
                blocks.Remove(id);
                mapHeaders517.Remove(id);
                oldBlocks508.Remove(id);
            }
            ForgetResourceBinaryBlocks577(dead);
            if (major)
                oldBlocks508.Clear();
            else
                foreach (long id in marked)
                    if (id <= highWaterIdentity508)
                        oldBlocks508.Add(id);
            highWaterIdentity508 = identity;
            minorCollections508 = nextMinor;
            completedCollections508 = completed;
            reclaimedPayloadWords508 = totalReclaimed;
            reclaimedAllocationWords567 = totalReclaimedAllocation;

            return new(
                major,
                reclaimed,
                completed,
                nextMinor
            )
            {
                ReclaimedAllocationWords567 = reclaimedAllocation
            };
        }
    }
}
