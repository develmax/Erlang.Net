using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class NifNativeAllocationOwner
{
    private readonly object gate = new();
    private readonly Dictionary<nint, nuint> owned = new();
    private NifNativeAllocationSnapshot snapshot;

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
            }

            return pointer;
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
            owned.Remove(pointer);
            NativeMemory.Free((void*)pointer);
            snapshot = next;
        }
    }

    internal void ReclaimAll()
    {
        lock (gate)
        {
            var next = snapshot with
            {
                Count = 0,
                RequestedBytes = 0,
                BackingRequestBytes = 0,
                ReleasedRequestedBytes = checked(snapshot.ReleasedRequestedBytes + snapshot.RequestedBytes),
                ReleasedBackingRequestBytes = checked(snapshot.ReleasedBackingRequestBytes + snapshot.BackingRequestBytes),
                ReleasedAllocations = checked(snapshot.ReleasedAllocations + snapshot.Count)
            };
            foreach (nint pointer in owned.Keys)
                NativeMemory.Free((void*)pointer);
            owned.Clear();
            snapshot = next;
        }
    }

    private static ulong BackingRequest(nuint size) => size == 0 ? NifNativeAllocationParameters.MinimumBackingRequestBytes : size;
}
