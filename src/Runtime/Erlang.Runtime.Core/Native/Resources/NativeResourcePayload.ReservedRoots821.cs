namespace Erlang;

internal sealed partial class NativeResourcePayload524
{
    // Caller owns a genuine source invocation/borrow until admission. This
    // allocates only the lease object; it does not add physical native refs.
    internal NativeResourceRootLease524 ReserveRoot821()
    {
        var lease = new NativeResourceRootLease524(this);
        lock (gate) RequireLive524();

        return lease;
    }

    // No allocation, discovery, callback or waiting on a scheduler lane. The
    // same real lease can already be installed in a private target header.
    internal void AdmitReservedRoot821(NativeResourceRootLease524 lease)
    {
        lock (gate)
        {
            if (!lease.BelongsTo821(this)) throw new InvalidOperationException(NativeResource524Names.Foreign);
            if (lease.State821 != NativeResourceRootLeaseState821.Reserved)
                throw new InvalidOperationException(NativeResource524Names.Unbalanced);
            RequireLive524();
            long next = checked(roots + 1);
            _ = checked(native + next);
            roots = next;
            UpdatePhysicalCount527();
            lease.SetState821(NativeResourceRootLeaseState821.Admitted);
        }
    }

    // Header retirement races final admission under this ONE payload gate.
    // Cancelling a reserved header cannot later resurrect a native share.
    internal void ReleaseRootLease821(NativeResourceRootLease524 lease, bool guestGc)
    {
        bool retire = false;
        lock (gate)
        {
            if (!lease.BelongsTo821(this)) throw new InvalidOperationException(NativeResource524Names.Foreign);
            if (lease.State821 == NativeResourceRootLeaseState821.Retired) return;
            if (lease.State821 == NativeResourceRootLeaseState821.Admitted)
            {
                if (roots == 0) throw new InvalidOperationException(NativeResource524Names.Unbalanced);
                roots--;
                UpdatePhysicalCount527();
                retire = QueueIfDead524();
            }
            lease.SetState821(NativeResourceRootLeaseState821.Retired);
        }
        // Existing actual dispatcher owns native destructor execution. It is
        // never invoked inline while holding the payload or runtime monitor.
        if (retire) domain.Enqueue524(this, guestGc);
    }
}
