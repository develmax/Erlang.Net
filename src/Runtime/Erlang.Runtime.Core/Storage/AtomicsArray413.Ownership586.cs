namespace Erlang;

internal sealed unsafe partial class AtomicsArray413
{
    private readonly TaskCompletionSource retired586 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task Retired586 => retired586.Task;
    private RuntimeAtomicsOwner413? backingOwner586;
    private ProcessRuntime? backingRuntime586;
    private long producers586, guestRoots586, hostRoots586;
    private bool ownershipStarted586, retirementScheduled586, identityBound586;

    internal ProcessRuntime Runtime586
    {
        get
        {
            lock (gate) return backingRuntime586 ?? throw new InvalidOperationException(AtomicsOwnership586Names.Foreign);
        }
    }

    internal void BindOwner586(RuntimeAtomicsOwner413 owner, ProcessRuntime runtime)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed || freed, this);
            if (backingOwner586 is not null && (!ReferenceEquals(backingOwner586, owner) || !ReferenceEquals(backingRuntime586, runtime)))
                throw new InvalidOperationException(AtomicsOwnership586Names.Foreign);
            backingOwner586 = owner;
            backingRuntime586 = runtime;
        }
    }

    internal AtomicsBackingLease586 RetainBacking586(AtomicsLeaseKind586 kind)
    {
        // Allocate lease before committing any counter/native-prefix mutation.
        var lease = new AtomicsBackingLease586(this, kind);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed || freed || retirementScheduled586, AtomicsOwnership586Names.Retired);
            if (backingOwner586 is null) throw new InvalidOperationException(AtomicsOwnership586Names.Foreign);
            long producer = producers586, guest = guestRoots586, host = hostRoots586;
            switch (kind)
            {
                case AtomicsLeaseKind586.Producer: producer = checked(producer + 1); break;
                case AtomicsLeaseKind586.Guest: guest = checked(guest + 1); break;
                case AtomicsLeaseKind586.Host: host = checked(host + 1); break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            long physicalRefs = checked(guest + host);
            producers586 = producer;
            guestRoots586 = guest;
            hostRoots586 = host;
            ownershipStarted586 = true;
            Header->ReferenceCount = physicalRefs;
        }

        return lease;
    }

    internal void ReleaseBacking586(AtomicsLeaseKind586 kind)
    {
        RuntimeAtomicsOwner413? retire;
        lock (gate)
        {
            switch (kind)
            {
                case AtomicsLeaseKind586.Producer:
                    if (producers586 == 0) throw new InvalidOperationException(AtomicsOwnership586Names.Underflow);
                    producers586--; break;
                case AtomicsLeaseKind586.Guest:
                    if (guestRoots586 == 0) throw new InvalidOperationException(AtomicsOwnership586Names.Underflow);
                    guestRoots586--; break;
                case AtomicsLeaseKind586.Host:
                    if (hostRoots586 == 0) throw new InvalidOperationException(AtomicsOwnership586Names.Underflow);
                    hostRoots586--; break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (memory != 0) Header->ReferenceCount = checked(guestRoots586 + hostRoots586);
            Monitor.PulseAll(gate);
            retire = ScheduleIfUnowned586();
        }
        // PARENT OWNER SEAM: enqueue only. Never inline Retire/Close while the
        // releasing caller may hold a process heap gate. Queue failure retains
        // the allocation and must be recorded by the owner's final join.
        retire?.EnqueueRetirement586(this);
    }

    private RuntimeAtomicsOwner413? ScheduleIfUnowned586()
    {
        if (!ownershipStarted586 || retirementScheduled586 || closed || freed
            || active != 0 || producers586 != 0 || guestRoots586 != 0 || hostRoots586 != 0)
            return null;
        retirementScheduled586 = true;

        return backingOwner586;
    }

    internal nint ReadMagicBinary586(AtomicsBackingLease586 lease)
    {
        lock (gate)
        {
            RequireBackingRead586(lease);

            return memory;
        }
    }

    internal IReadOnlyList<uint> ReadMagicNumbers586(AtomicsBackingLease586 lease)
    {
        lock (gate)
        {
            RequireBackingRead586(lease);
            if (!identityBound586) throw new InvalidOperationException(AtomicsOwnership586Names.Identity);

            return Array.AsReadOnly(new uint[] { Header->Reference0, Header->Reference1, Header->Reference2 });
        }
    }

    private void RequireBackingRead586(AtomicsBackingLease586 lease)
    {
        if (!ReferenceEquals(lease.Value, this)) throw new InvalidOperationException(AtomicsOwnership586Names.Foreign);
        ObjectDisposedException.ThrowIf(closed || freed || memory == 0, AtomicsOwnership586Names.Retired);
        if (Header->Flags != NativeResourceMagic527.Magic || Header->Destructor != DestructorAddress
            || Header->AllocationKind != AtomicsNames413.AllocationKind
            || Header->OriginalSize != (nuint)(AllocatedBytes - AtomicsOwnership586Names.BinaryPrefixBytes)
            || Header->ApparentSize != (nuint)(AllocatedBytes - AtomicsOwnership586Names.DataOffsetBytes)
            || Header->ReferenceCount != checked(guestRoots586 + hostRoots586))
            throw new InvalidOperationException(AtomicsOwnership586Names.InvalidPrefix);
    }

    // Parent calls after actual physical Close outside owner/array/table gates.
    internal void ForgetReference586() => backingRuntime586?.ReferenceResources.ForgetAtomics586(this);
}
