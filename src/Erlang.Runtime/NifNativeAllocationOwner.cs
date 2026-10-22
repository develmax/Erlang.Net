using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe partial class NifNativeAllocationOwner
{
    private readonly object gate = new();
    private readonly Dictionary<nint, nuint> owned = new();
    private NifNativeAllocationSnapshot snapshot;
    private NifNativeAllocationDomain? domain;
    private long domainMembership;
    private bool domainEnded;

    internal int Count
    {
        get
        {
            lock (gate)
                return owned.Count;
        }
    }

    internal NifNativeAllocationSnapshot Snapshot
    {
        get
        {
            lock (gate)
                return snapshot;
        }
    }

    internal void JoinDomain(NifNativeAllocationDomain allocationDomain)
    {
        ArgumentNullException.ThrowIfNull(allocationDomain);
        lock (gate)
        {
            if (domainEnded)
                throw new InvalidOperationException(NifNativeAllocationDomainDiagnostics.ClosedMembership);
            if (domain is not null)
            {
                if (!ReferenceEquals(domain, allocationDomain))
                    throw new InvalidOperationException(NifNativeAllocationDomainDiagnostics.DifferentDomain);

                return;
            }
            if (snapshot != default)
                throw new InvalidOperationException(NifNativeAllocationDomainDiagnostics.ExistingAllocationHistory);
            long membership = allocationDomain.Join();
            domain = allocationDomain;
            domainMembership = membership;
        }
    }

    internal bool TryEndDomainMembership()
    {
        lock (gate)
        {
            if (domain is null || domainEnded)
                return true;
            if (snapshot.Count != 0 || snapshot.RequestedBytes != 0 || snapshot.BackingRequestBytes != 0)
                return false;
            domain.EndMembership(domainMembership, snapshot);
            domainEnded = true;

            return true;
        }
    }

    internal nint Allocate(nuint size)
    {
        lock (gate)
        {
            ulong requested = size;
            ulong backing = BackingRequest(size);
            ulong requestedBytes = checked(snapshot.RequestedBytes + requested);
            ulong backingBytes = checked(snapshot.BackingRequestBytes + backing);
            var next = snapshot with
            {
                Count = checked(snapshot.Count + 1),
                RequestedBytes = requestedBytes,
                BackingRequestBytes = backingBytes,
                PeakRequestedBytes = Math.Max(snapshot.PeakRequestedBytes, requestedBytes),
                PeakBackingRequestBytes = Math.Max(snapshot.PeakBackingRequestBytes, backingBytes),
                AllocatedRequestedBytes = checked(snapshot.AllocatedRequestedBytes + requested),
                AllocatedBackingRequestBytes = checked(snapshot.AllocatedBackingRequestBytes + backing),
                SuccessfulAllocations = checked(snapshot.SuccessfulAllocations + 1)
            };
            using var change = domain?.BeginChange(domainMembership, snapshot, next);
            nint pointer = (nint)NativeMemory.Alloc((nuint)backing);
            if (pointer != 0)
            {
                try
                {
                    owned.Add(pointer, size);
                }
                catch
                {
                    NativeMemory.Free((void*)pointer);
                    throw;
                }
                snapshot = next;
                change?.Commit();
            }

            return pointer;
        }
    }

    // Byte histories settle the full old/new requested extent, including in-place resize.
    // Block admissions/disposals stay unchanged; these are not physical/ERTS API totals.
    internal nint Reallocate(nint pointer, nuint size)
    {
        if (pointer == 0)
            return Allocate(size);
        lock (gate)
        {
            if (!owned.TryGetValue(pointer, out nuint previous))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignAllocationReallocate);
            ulong backing = BackingRequest(size);
            var next = snapshot with
            {
                RequestedBytes = checked(snapshot.RequestedBytes - (ulong)previous + (ulong)size),
                BackingRequestBytes = checked(snapshot.BackingRequestBytes - BackingRequest(previous) + backing),
                AllocatedRequestedBytes = checked(snapshot.AllocatedRequestedBytes + (ulong)size),
                ReleasedRequestedBytes = checked(snapshot.ReleasedRequestedBytes + (ulong)previous),
                AllocatedBackingRequestBytes = checked(snapshot.AllocatedBackingRequestBytes + backing),
                ReleasedBackingRequestBytes = checked(snapshot.ReleasedBackingRequestBytes + BackingRequest(previous))
            };
            next = next with
            {
                PeakRequestedBytes = Math.Max(snapshot.PeakRequestedBytes, next.RequestedBytes),
                PeakBackingRequestBytes = Math.Max(snapshot.PeakBackingRequestBytes, next.BackingRequestBytes)
            };
            owned.EnsureCapacity(checked(owned.Count + 1));
            using var change = domain?.BeginChange(domainMembership, snapshot, next);
            nint resized = (nint)NativeMemory.Realloc((void*)pointer, (nuint)backing);
            if (resized == 0)
                return 0;
            if (resized == pointer)
                owned[pointer] = size;
            else
            {
                // Capacity and all arithmetic were checked before destructive native resize.
                // The default nint comparer has no user callback; no rollback can revive pointer.
                owned.Add(resized, size);
                owned.Remove(pointer);
            }
            snapshot = next;
            change?.Commit();

            return resized;
        }
    }

    internal void Free(nint pointer)
    {
        if (pointer == 0)
            return;
        lock (gate)
        {
            if (!owned.TryGetValue(pointer, out nuint size))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignAllocationFree);
            var next = snapshot with
            {
                Count = checked(snapshot.Count - 1),
                RequestedBytes = checked(snapshot.RequestedBytes - (ulong)size),
                BackingRequestBytes = checked(snapshot.BackingRequestBytes - BackingRequest(size)),
                ReleasedRequestedBytes = checked(snapshot.ReleasedRequestedBytes + (ulong)size),
                ReleasedBackingRequestBytes = checked(snapshot.ReleasedBackingRequestBytes + BackingRequest(size)),
                ReleasedAllocations = checked(snapshot.ReleasedAllocations + 1)
            };
            using var change = domain?.BeginChange(domainMembership, snapshot, next);
            owned.Remove(pointer);
            NativeMemory.Free((void*)pointer);
            snapshot = next;
            change?.Commit();
        }
    }

    internal void ReclaimAll()
    {
        lock (gate)
        {
            if (owned.Count == 0)
                return;
            var next = snapshot with
            {
                Count = 0,
                RequestedBytes = 0,
                BackingRequestBytes = 0,
                ReleasedRequestedBytes = checked(snapshot.ReleasedRequestedBytes + snapshot.RequestedBytes),
                ReleasedBackingRequestBytes = checked(snapshot.ReleasedBackingRequestBytes + snapshot.BackingRequestBytes),
                ReleasedAllocations = checked(snapshot.ReleasedAllocations + snapshot.Count)
            };
            using var change = domain?.BeginChange(domainMembership, snapshot, next);
            foreach (nint pointer in owned.Keys)
                NativeMemory.Free((void*)pointer);
            owned.Clear();
            snapshot = next;
            change?.Commit();
        }
    }

    private static ulong BackingRequest(nuint size) => size == 0 ? NifNativeAllocationParameters.MinimumBackingRequestBytes : size;
}
