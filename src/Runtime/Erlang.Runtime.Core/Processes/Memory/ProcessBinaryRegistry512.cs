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
        HashSet<OrdinaryBinaryBacking>? ordinaryBinaries = null;
        HashSet<NativeResourcePayload524>? native = null;
        HashSet<AtomicsGuestRoot413>? atomics586 = null;
        HashSet<CountersGuestRoot421>? counters596 = null;
        HashSet<ReMagicBacking597>? regex597 = null;
        HashSet<BinaryProgramBacking598>? binary598 = null;
        long visits = 0;
        var pending = new Stack<Term>();
        pending.Push(graph);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value))
                continue;
            visits++;
            // This exact immutable closure cannot contain ANY binary/native
            // backing. Keep the real lease/publication/rollback bookkeeping;
            // only omit its proven empty descendant walk.
            if (value is Cons or TupleTerm && owner is ProcessTermHeap506 closureHeap && Monitor.IsEntered(closureHeap.Gate)
                && ProcessTermHeap506.Storage(value) is ProcessTermSequence506 { HasImmutablePlainClosure550: true }
                && closureHeap.IsOwnedLocal(value)) continue;
            if (value is ResourceReferenceTerm { OwnedStorage562: { } storage } && !ReferenceEquals(storage.Owner, owner))
                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
            if (NativeResourceRoots524.Direct524(value, (owner as ProcessTermHeap506)?.Runtime524) is { } resource
                && !ProcessTermHeap506.HasOwnedMagicReference576(value))
                (native ??= new(ReferenceEqualityComparer.Instance)).Add(resource);
            if (AtomicsRoots586.Direct586(value, (owner as ProcessTermHeap506)?.Runtime524) is { } atomics
                && !ProcessTermHeap506.HasOwnedMagicReference576(value))
                (atomics586 ??= new(ReferenceEqualityComparer.Instance)).Add(atomics);
            if (CountersRoots596.Direct596(value, (owner as ProcessTermHeap506)?.Runtime524) is { } counters
                && !ProcessTermHeap506.HasOwnedMagicReference576(value))
                (counters596 ??= new(ReferenceEqualityComparer.Instance)).Add(counters);
            if (ReMagicRoots597.Direct597(value, (owner as ProcessTermHeap506)?.Runtime524) is { } regex
                && !ProcessTermHeap506.HasOwnedMagicReference576(value))
                (regex597 ??= new(ReferenceEqualityComparer.Instance)).Add(regex);
            if (BinaryProgramRoots598.Direct598(value, (owner as ProcessTermHeap506)?.Runtime524) is { } program
                && !ProcessTermHeap506.HasOwnedMagicReference576(value))
                (binary598 ??= new(ReferenceEqualityComparer.Instance)).Add(program);
            if (value is BitString binary)
            {
                if (binary.OwnedStorage577 is null && binary.ResourceBacking577.NativeStorage524 is OrdinaryBinaryBacking ordinary)
                {
                    if (owner is ProcessTermHeap506 heap && !ReferenceEquals(heap.Runtime524, ordinary.Runtime))
                        throw new NotSupportedException(NativeResource524Names.Foreign);
                    (ordinaryBinaries ??= new(ReferenceEqualityComparer.Instance)).Add(ordinary);
                }
                BinaryPayloadHandle512 handle = binary.CaptureBinaryPayload512();
                backings.TryAdd(handle.Identity, handle);
            }
            else
                foreach (Term child in NativeMapOwnedRoots510.Children(value))
                    pending.Push(child);
        }
        var retained = new IDisposable[(ordinaryBinaries?.Count ?? 0) + (native?.Count ?? 0) + (atomics586?.Count ?? 0) + (counters596?.Count ?? 0) + (regex597?.Count ?? 0) + (binary598?.Count ?? 0)];
        int admitted = 0;
        ProcessBinaryLease512 lease;
        try
        {
            if (ordinaryBinaries is not null)
                foreach (var backing in ordinaryBinaries) retained[admitted++] = backing.Retain();
            if (native is not null)
                foreach (NativeResourcePayload524 payload in native) retained[admitted++] = payload.RetainRoot524();
            if (atomics586 is not null)
                foreach (AtomicsGuestRoot413 root in atomics586) retained[admitted++] = root.RetainGuest586();
            if (counters596 is not null)
                foreach (CountersGuestRoot421 root in counters596) retained[admitted++] = root.RetainGuest596();
            if (regex597 is not null)
                foreach (var backing in regex597) retained[admitted++] = backing.Retain(ReMagicLeaseKind597.Guest);
            if (binary598 is not null)
                foreach (var backing in binary598) retained[admitted++] = backing.Retain598(BinaryProgramLeaseKind598.Guest);
            lease = new ProcessBinaryLease512(
                this,
                owner,
                backings.Values.ToArray(),
                retained.Length == 0 ? null : new NativeResourceRootSet524(retained)
            );
        }
        catch (Exception original)
        {
            Exception? cleanupFailure = null;
            for (int index = admitted - 1; index >= 0; index--)
                try
                {
                    retained[index].Dispose();
                }
                catch (Exception error)
                {
                    cleanupFailure ??= error;
                }
            if (cleanupFailure is not null)
                throw new AggregateException(original, cleanupFailure);
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
