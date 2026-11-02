namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal OwnedPhysicalWordSnapshot526 PhysicalWordSnapshot526
    {
        get
        {
            lock (Gate)
                return new(
                    slabs.Sum(slab => (long)slab.Cells.Length),
                    slabs.Sum(slab => (long)slab.Cells.RootedTerms526),
                    slabs.Sum(slab => (long)slab.Cells.RawHeaders526),
                    slabs.Sum(slab => (long)slab.Cells.RootedTerms526 + slab.Cells.RawHeaders526 + slab.Cells.RawNumericWords556),
                    blocks.Count,
                    slabs.Sum(slab => (long)slab.Cells.RootedTerms526),
                    slabs.Sum(slab => (long)slab.Cells.RawNumericWords556)
                );
        }
    }

    internal ProcessTermSequence506 AllocateNativeHeader526(nuint[] words)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        nuint[] snapshot = (nuint[])words.Clone();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(snapshot.Length);

        return AllocateEncoded526([], snapshot);
    }

    internal void ReleaseRegisteredHeader526(ProcessTermSequence506 payload)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (!ReferenceEquals(payload.Owner, this) || payload.Count != 0
            || !blocks.TryGetValue(payload.Identity, out var current) || !ReferenceEquals(current, payload))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        ReleaseAllocation519(payload.Slab, payload.AllocationOffset526, payload.AllocationWords526);
        blocks.Remove(payload.Identity);
        ReleaseLiteralBlock801(payload.Identity);
        oldBlocks508.Remove(payload.Identity);
        mapHeaders517.Remove(payload.Identity);
    }
}
