namespace Erlang;

internal sealed unsafe partial class CountersArray421
{
    private readonly TaskCompletionSource retired596 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task Retired596 => retired596.Task;
    private RuntimeCountersOwner421? backingOwner596;
    private ProcessRuntime? backingRuntime596;
    private long producers596, guestRoots596, hostRoots596;
    private bool ownershipStarted596, retirementScheduled596, identityBound596;

    internal ProcessRuntime Runtime596
    {
        get
        {
            lock (gate) return backingRuntime596 ?? throw new InvalidOperationException(CountersOwnership596Names.Foreign);
        }
    }

    internal void BindOwner596(RuntimeCountersOwner421 owner, ProcessRuntime runtime)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed || freed, this);
            if (backingOwner596 is not null && (!ReferenceEquals(backingOwner596, owner) || !ReferenceEquals(backingRuntime596, runtime)))
                throw new InvalidOperationException(CountersOwnership596Names.Foreign);
            backingOwner596 = owner;
            backingRuntime596 = runtime;
        }
    }

    internal CountersBackingLease596 RetainBacking596(CountersLeaseKind596 kind)
    {
        // Allocate lease before committing any counter/native-prefix mutation.
        var lease = new CountersBackingLease596(this, kind);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed || freed || retirementScheduled596, CountersOwnership596Names.Retired);
            if (backingOwner596 is null) throw new InvalidOperationException(CountersOwnership596Names.Foreign);
            long producer = producers596, guest = guestRoots596, host = hostRoots596;
            switch (kind)
            {
                case CountersLeaseKind596.Producer: producer = checked(producer + 1); break;
                case CountersLeaseKind596.Guest: guest = checked(guest + 1); break;
                case CountersLeaseKind596.Host: host = checked(host + 1); break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            long physicalRefs = checked(guest + host);
            producers596 = producer;
            guestRoots596 = guest;
            hostRoots596 = host;
            ownershipStarted596 = true;
            Header->ReferenceCount = physicalRefs;
        }

        return lease;
    }

    internal void ReleaseBacking596(CountersLeaseKind596 kind)
    {
        RuntimeCountersOwner421? retire;
        lock (gate)
        {
            switch (kind)
            {
                case CountersLeaseKind596.Producer:
                    if (producers596 == 0) throw new InvalidOperationException(CountersOwnership596Names.Underflow);
                    producers596--; break;
                case CountersLeaseKind596.Guest:
                    if (guestRoots596 == 0) throw new InvalidOperationException(CountersOwnership596Names.Underflow);
                    guestRoots596--; break;
                case CountersLeaseKind596.Host:
                    if (hostRoots596 == 0) throw new InvalidOperationException(CountersOwnership596Names.Underflow);
                    hostRoots596--; break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (memory != 0) Header->ReferenceCount = checked(guestRoots596 + hostRoots596);
            Monitor.PulseAll(gate);
            retire = ScheduleIfUnowned596();
        }
        // PARENT OWNER SEAM: enqueue only. Never inline Retire/Close while the
        // releasing caller may hold a process heap gate. Queue failure retains
        // the allocation and must be recorded by the owner's final join.
        retire?.EnqueueRetirement596(this);
    }

    private RuntimeCountersOwner421? ScheduleIfUnowned596()
    {
        if (!ownershipStarted596 || retirementScheduled596 || closed || freed
            || active != 0 || producers596 != 0 || guestRoots596 != 0 || hostRoots596 != 0)
            return null;
        retirementScheduled596 = true;

        return backingOwner596;
    }

    internal nint ReadMagicBinary596(CountersBackingLease596 lease)
    {
        lock (gate)
        {
            RequireBackingRead596(lease);

            return memory;
        }
    }

    internal IReadOnlyList<uint> ReadMagicNumbers596(CountersBackingLease596 lease)
    {
        lock (gate)
        {
            RequireBackingRead596(lease);
            if (!identityBound596) throw new InvalidOperationException(CountersOwnership596Names.Identity);

            return Array.AsReadOnly(new uint[] { Header->Reference0, Header->Reference1, Header->Reference2 });
        }
    }

    private void RequireBackingRead596(CountersBackingLease596 lease)
    {
        if (!ReferenceEquals(lease.Value, this)) throw new InvalidOperationException(CountersOwnership596Names.Foreign);
        ObjectDisposedException.ThrowIf(closed || freed || memory == 0, CountersOwnership596Names.Retired);
        if (Header->Flags != NativeResourceMagic527.Magic || Header->Destructor != DestructorAddress
            || Header->AllocationKind != CountersNames421.AllocationKind
            || Header->OriginalSize != (nuint)(AllocatedBytes - CountersOwnership596Names.BinaryPrefixBytes)
            || Header->ApparentSize != (nuint)(AllocatedBytes - CountersOwnership596Names.DataOffsetBytes)
            || Header->ReferenceCount != checked(guestRoots596 + hostRoots596))
            throw new InvalidOperationException(CountersOwnership596Names.InvalidPrefix);
    }

    // Parent calls after actual physical Close outside owner/array/table gates.
    internal void ForgetReference596() => backingRuntime596?.ReferenceResources.ForgetCounters596(this);
}
