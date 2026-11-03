namespace Erlang;

internal sealed partial class NifNativeAllocationDomain
{
    private readonly object gate = new();
    private readonly Dictionary<long, NifNativeAllocationSnapshot> owners = new();
    private long nextMembership;
    private NifNativeAllocationSnapshot snapshot;

    internal NifNativeAllocationDomainSnapshot Snapshot
    {
        get
        {
            Monitor.Enter(gate);
            try
            {
                return new(owners.Count, snapshot);
            }
            finally
            {
                ReleaseGate();
            }
        }
    }

    internal long Join()
    {
        Monitor.Enter(gate);
        try
        {
            ObjectDisposedException.ThrowIf(finalRetirement528, NifNativeAllocationArena528Names.Closed);
            long membership = checked(nextMembership + 1);
            owners.Add(membership, default);
            nextMembership = membership;

            return membership;
        }
        finally
        {
            ReleaseGate();
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
            ReleaseGate();
            throw;
        }
    }

    internal void EndMembership(long membership, NifNativeAllocationSnapshot owner)
    {
        Monitor.Enter(gate);
        try
        {
            RequireOwner(membership, owner);
            if (owner.Count != 0 || owner.RequestedBytes != 0 || owner.BackingRequestBytes != 0)
                throw new InvalidOperationException(NifNativeAllocationDomainDiagnostics.NonemptyMembership);
            long nextRevision528 = checked(ownerRevision528 + 1);
            owners.Remove(membership);
            actualOwners528.Remove(membership);
            ownerRevision528 = nextRevision528;
            scanOwners434.Remove(membership);
        }
        finally
        {
            ReleaseGate();
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

    internal void EndChange() => ReleaseGate();
}
