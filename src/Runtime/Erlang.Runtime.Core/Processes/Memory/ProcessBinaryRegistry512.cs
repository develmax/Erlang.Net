namespace Erlang;

internal sealed class ProcessBinaryRegistry512
{
    private readonly object gate = new();
    private long visitedTerms514;
    private long retainOperations514;
    private long liveBackingReferences514;
    private long peakBackingReferences514;

    internal ProcessBinaryTraversalSnapshot514 TraversalSnapshot514
    {
        get
        {
            lock (gate)
                return new(
                    visitedTerms514,
                    retainOperations514,
                    liveBackingReferences514,
                    peakBackingReferences514
                );
        }
    }
    private readonly HashSet<ProcessBinaryLease512> leases = new();
    private readonly Dictionary<object, int> frames = new(ReferenceEqualityComparer.Instance);

    internal ProcessBinaryFrame512 Enter512(object owner)
    {
        var frame = new ProcessBinaryFrame512(this, owner);
        lock (gate)
            frames[owner] = checked(frames.GetValueOrDefault(owner) + 1);

        return frame;
    }

    internal ProcessBinaryLease512 Retain512(object owner, Term graph)
    {
        // Caller holds the guest heap gate/root during this graph walk. No
        // registry gate is held while reading term children or their backing.
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var backings = new Dictionary<object, BinaryPayloadHandle512>(ReferenceEqualityComparer.Instance);
        HashSet<NativeResourcePayload524>? native = null;
        long visits = 0;
        var pending = new Stack<Term>();
        pending.Push(graph);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value))
                continue;
            visits++;
            if (NativeResourceRoots524.Direct524(value) is { } resource)
                (native ??= new(ReferenceEqualityComparer.Instance)).Add(resource);
            if (value is BitString binary)
            {
                BinaryPayloadHandle512 handle = binary.CaptureBinaryPayload512();
                backings.TryAdd(handle.Identity, handle);
            }
            else
                foreach (Term child in NativeMapOwnedRoots510.Children(value))
                    pending.Push(child);
        }
        var retained = new IDisposable[native?.Count ?? 0];
        int admitted = 0;
        ProcessBinaryLease512 lease;
        try
        {
            if (native is not null)
                foreach (NativeResourcePayload524 payload in native) retained[admitted++] = payload.RetainRoot524();
            lease = new ProcessBinaryLease512(
                this,
                owner,
                backings.Values.ToArray(),
                retained.Length == 0 ? null : new NativeResourceRootSet524(retained)
            );
        }
        catch
        {
            for (int index = admitted - 1; index >= 0; index--) retained[index].Dispose();
            throw;
        }
        try
        {
            lock (gate)
            {
                long nextVisits = checked(visitedTerms514 + visits);
                long nextOperations = checked(retainOperations514 + 1);
                long nextReferences = checked(liveBackingReferences514 + lease.Handles.Length);
                leases.Add(lease);
                visitedTerms514 = nextVisits;
                retainOperations514 = nextOperations;
                liveBackingReferences514 = nextReferences;
                peakBackingReferences514 = Math.Max(peakBackingReferences514, nextReferences);
            }

            return lease;
        }
        catch
        {
            lease.Clear512();
            throw;
        }
    }

    internal void Release512(ProcessBinaryLease512 lease)
    {
        lock (gate)
        {
            if (!leases.Remove(lease))
                return;
            liveBackingReferences514 -= lease.Handles.Length;
            lease.Clear512();
        }
    }

    internal void Leave512(object owner)
    {
        lock (gate)
        {
            int remaining = frames[owner] - 1;
            if (remaining == 0)
                frames.Remove(owner);
            else
                frames[owner] = remaining;
        }
    }

    internal ProcessBinarySnapshot512 Snapshot512(object? owner = null)
    {
        lock (gate)
        {
            long reference = 0, heap = 0, roots = 0;
            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (ProcessBinaryLease512 lease in leases)
            {
                if (owner is not null && !ReferenceEquals(owner, lease.Owner))
                    continue;
                foreach (BinaryPayloadHandle512 handle in lease.Handles)
                {
                    roots++;
                    if (!seen.Add(handle.Identity))
                        continue;
                    if (handle.ReferenceCounted)
                        reference += handle.CapacityBytes;
                    else
                        heap += handle.CapacityBytes;
                }
            }
            int frameCount = owner is null ? frames.Values.Sum() : frames.GetValueOrDefault(owner);

            return new(
                reference,
                heap,
                seen.Count,
                roots,
                frameCount
            );
        }
    }
}
