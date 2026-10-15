namespace Erlang;

internal sealed class NifNativeAllocationDomain
{
    private readonly object gate = new();
    private readonly Dictionary<long, NifNativeAllocationSnapshot> owners = new();
    private long nextMembership;
    private NifNativeAllocationSnapshot snapshot;

    internal NifNativeAllocationDomainSnapshot Snapshot
    {
        get
        {
            lock (gate)
                return new(owners.Count, snapshot);
        }
    }

    internal long Join()
    {
        lock (gate)
        {
            long membership = checked(nextMembership + 1);
            owners.Add(membership, default);
            nextMembership = membership;

            return membership;
        }
    }

    internal NifNativeAllocationDomainChange BeginChange(
        long membership,
        NifNativeAllocationSnapshot before,
        NifNativeAllocationSnapshot after
    )
    {
        Monitor.Enter(gate);
        try
        {
            RequireOwner(membership, before);
            int count = checked(snapshot.Count - before.Count + after.Count);
            ulong requested = checked(snapshot.RequestedBytes - before.RequestedBytes + after.RequestedBytes);
            ulong backing = checked(snapshot.BackingRequestBytes - before.BackingRequestBytes + after.BackingRequestBytes);
            var aggregate = snapshot with
            {
                Count = count,
                RequestedBytes = requested,
                BackingRequestBytes = backing,
                PeakRequestedBytes = Math.Max(snapshot.PeakRequestedBytes, requested),
                PeakBackingRequestBytes = Math.Max(snapshot.PeakBackingRequestBytes, backing),
                AllocatedRequestedBytes = checked(snapshot.AllocatedRequestedBytes + checked(after.AllocatedRequestedBytes - before.AllocatedRequestedBytes)),
                ReleasedRequestedBytes = checked(snapshot.ReleasedRequestedBytes + checked(after.ReleasedRequestedBytes - before.ReleasedRequestedBytes)),
                AllocatedBackingRequestBytes = checked(snapshot.AllocatedBackingRequestBytes + checked(after.AllocatedBackingRequestBytes - before.AllocatedBackingRequestBytes)),
                ReleasedBackingRequestBytes = checked(snapshot.ReleasedBackingRequestBytes + checked(after.ReleasedBackingRequestBytes - before.ReleasedBackingRequestBytes)),
                SuccessfulAllocations = checked(snapshot.SuccessfulAllocations + checked(after.SuccessfulAllocations - before.SuccessfulAllocations)),
                ReleasedAllocations = checked(snapshot.ReleasedAllocations + checked(after.ReleasedAllocations - before.ReleasedAllocations))
            };

            return new(
                this,
                membership,
                after,
                aggregate
            );
        }
        catch
        {
            Monitor.Exit(gate);
            throw;
        }
    }

    internal void EndMembership(long membership, NifNativeAllocationSnapshot owner)
    {
        lock (gate)
        {
            RequireOwner(membership, owner);
            if (owner.Count != 0 || owner.RequestedBytes != 0 || owner.BackingRequestBytes != 0)
                throw new InvalidOperationException(NifNativeAllocationDomainDiagnostics.NonemptyMembership);
            owners.Remove(membership);
        }
    }

    private void RequireOwner(long membership, NifNativeAllocationSnapshot expected)
    {
        if (!owners.TryGetValue(membership, out var actual))
            throw new InvalidOperationException(NifNativeAllocationDomainDiagnostics.UnknownMembership);
        if (actual != expected)
            throw new InvalidOperationException(NifNativeAllocationDomainDiagnostics.SnapshotMismatch);
    }

    // BeginChange holds gate and has already checked membership and all arithmetic.
    // Updating an existing long-key slot and assigning value structs allocate nothing.
    internal void CommitChange(
        long membership,
        NifNativeAllocationSnapshot owner,
        NifNativeAllocationSnapshot aggregate
    )
    {
        owners[membership] = owner;
        snapshot = aggregate;
    }

    internal void EndChange() => Monitor.Exit(gate);
}
