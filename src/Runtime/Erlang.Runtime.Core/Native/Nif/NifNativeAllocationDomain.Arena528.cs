namespace Erlang;

internal sealed partial class NifNativeAllocationDomain
{
    private readonly Dictionary<long, NifNativeAllocationOwner> actualOwners528 = new();
    private bool finalRetirement528, finalized528;
    private long ownerRevision528;
    internal bool Finalized528
    {
        get
        {
            lock (gate) return finalized528;
        }
    }

    internal void RegisterOwner528(long membership, NifNativeAllocationOwner owner)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(finalRetirement528, NifNativeAllocationArena528Names.Closed);
            long next = checked(ownerRevision528 + 1);
            actualOwners528.Add(membership, owner);
            ownerRevision528 = next;
        }
    }

    internal NifNativeAllocationMutation528 BeginPointerMutation528(long membership,
        NifNativeAllocationOwner owner, int extra)
    {
        if (!Monitor.IsEntered(gate)) throw new InvalidOperationException(NifNativeAllocationArena528Names.Gate);
        if (!actualOwners528.TryGetValue(membership, out var actual) || !ReferenceEquals(actual, owner))
            throw new InvalidOperationException(NifNativeAllocationArena528Names.UnknownOwner);
        if (extra != 0 && finalRetirement528)
            throw new ObjectDisposedException(NifNativeAllocationArena528Names.Closed);

        // Object allocation and capacity reservation precede all destructive
        // native resize/free operations. Global pointer gate stays held until
        // both the owner ledger and arena snapshot have committed.
        return new(owner, extra);
    }

    internal void ImageAttached528()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(finalRetirement528, NifNativeAllocationArena528Names.Closed);
            ownerRevision528 = checked(ownerRevision528 + 1);
        }
    }

    internal void ReclaimFinal528()
    {
        NifNativeAllocationOwner[] selected;
        while (true)
        {
            long revision;
            lock (gate)
            {
                if (finalized528) return;
                selected = actualOwners528.Values.ToArray();
                revision = ownerRevision528;
            }
            // Never acquire owner gates under the arena gate. A failed early
            // retirement attempt must not close a still-active image's arena.
            foreach (NifNativeAllocationOwner owner in selected)
                if (!owner.PhysicalImageClosed528)
                    throw new InvalidOperationException(NifNativeAllocationArena528Names.PhysicalClosure);
            lock (gate)
            {
                if (revision != ownerRevision528) continue;
                finalRetirement528 = true;
                break;
            }
        }
        foreach (NifNativeAllocationOwner owner in selected)
        {
            owner.ReclaimAll();
            owner.TryEndDomainMembership();
        }
        lock (gate)
        {
            if (snapshot.Count != 0 || actualOwners528.Count != 0 || owners.Count != 0)
                throw new InvalidOperationException(NifNativeAllocationArena528Names.PhysicalClosure);
            finalized528 = true;
        }
    }
}
