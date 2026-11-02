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
    // Root maps to its oldest eligible REAL active lease. The intrusive ring
    // contains only already registered leases; no separate cache owner exists.
    private readonly Dictionary<Term, ProcessBinaryLease512> singleBackingSummaries466 = new(ReferenceEqualityComparer.Instance);
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
        if (ImmediateTermRoot161.IsEmpty(graph))
            return RegisterLease161(new ProcessBinaryLease512(this, owner, []), 1);
        // Heap/dispatch/native-proof reads happen BEFORE the registry gate.
        // A hit copies one handle reference, never an existing lease or array.
        bool canSummarize466 = CanSummarizeSingleBacking466(owner, graph);
        if (canSummarize466 && SnapshotActiveSummary468(owner, graph) is { } summarized)
            return RegisterLease161(new ProcessBinaryLease512(this, owner, [summarized]), 1, graph);
        // Caller holds the guest heap gate/root during this graph walk. No
        // registry gate is held while reading term children or their backing.
        HashSet<Term>? seen = null;
        BinaryPayloadHandle512? firstBacking490 = null;
        Dictionary<object, BinaryPayloadHandle512>? backings = null;
        HashSet<OrdinaryBinaryBacking>? ordinaryBinaries = null;
        HashSet<NativeResourcePayload524>? native = null;
        HashSet<AtomicsGuestRoot413>? atomics586 = null;
        HashSet<CountersGuestRoot421>? counters596 = null;
        HashSet<ReMagicBacking597>? regex597 = null;
        HashSet<BinaryProgramBacking598>? binary598 = null;
        HashSet<PreparedLoaderOwner807>? prepared807 = null;
        long visits = 0;
        Stack<Term>? pending = null;
        Term? value = graph;
        do
        {
            if (owner is ProcessTermHeap506 literalHeap && Monitor.IsEntered(literalHeap.Gate)
                && literalHeap.IsAuthenticatedLiteral801(value)) continue;
            if (seen?.Add(value) == false)
                continue;
            visits++;
            // A portable wrapper or fresh root can contain an already-rooted
            // immutable tail. Qualify under H before registry metadata lookup.
            if (CanSummarizeSingleBacking466(owner, value)
                && SnapshotActiveSummary468(owner, value) is { } childBacking)
            {
                // No mutable lease array escapes the registry gate. Reading
                // backing identity and merging the real handle happens AFTER it.
                AddBacking490(ref firstBacking490, ref backings, childBacking);
                continue;
            }
            // This exact immutable closure cannot contain ANY binary/native
            // backing. Keep the real lease/publication/rollback bookkeeping;
            // only omit its proven empty descendant walk.
            if (value is Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429 && owner is ProcessTermHeap506 closureHeap && Monitor.IsEntered(closureHeap.Gate)
                && ProcessTermHeap506.Storage(value) is ProcessTermSequence506 immutableClosure
                && (immutableClosure.HasImmutablePlainClosure550 || immutableClosure.HasImmutableNoBinaryClosure192)
                && closureHeap.IsOwnedLocal(value) && closureHeap.CanUseImmutableClosure293(value, immutableClosure)) continue;
            if (value is Cons or TupleTerm or FunctionTerm or MapTerm or HamtBitmapTerm429 && owner is ProcessTermHeap506 literalClosureHeap && Monitor.IsEntered(literalClosureHeap.Gate)
                && ProcessTermHeap506.Storage(value) is ProcessTermSequence506 literalOnly
                && literalClosureHeap.IsOwnedLocal(value) && literalOnly.HasImmutableLiteralOnlyClosure132
                && literalClosureHeap.CanUseImmutableClosure293(value, literalOnly)) continue;
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
            if (PreparedLoaderRoots807.Direct807(value, (owner as ProcessTermHeap506)?.Runtime524) is { } prepared
                && !ProcessTermHeap506.HasOwnedMagicReference576(value))
                (prepared807 ??= new(ReferenceEqualityComparer.Instance)).Add(prepared);
            if (value is BitString binary)
            {
                if (binary.OwnedStorage577 is null && binary.ResourceBacking577.NativeStorage524 is OrdinaryBinaryBacking ordinary)
                {
                    if (owner is ProcessTermHeap506 heap && !ReferenceEquals(heap.Runtime524, ordinary.Runtime))
                        throw new NotSupportedException(NativeResource524Names.Foreign);
                    (ordinaryBinaries ??= new(ReferenceEqualityComparer.Instance)).Add(ordinary);
                }
                BinaryPayloadHandle512 handle = binary.CaptureBinaryPayload512();
                AddBacking490(ref firstBacking490, ref backings, handle);
            }
            else
                foreach (Term child in NativeMapOwnedRoots510.Children(value))
                {
                    if (pending is null)
                    {
                        // The root has already passed authentication and counted
                        // as visited. Seed it before admitting any actual child.
                        seen = new HashSet<Term>(ReferenceEqualityComparer.Instance) { graph };
                        pending = new Stack<Term>();
                    }
                    pending.Push(child);
                }
        }
        while (pending is not null && pending.TryPop(out value));
        var retained = new IDisposable[(ordinaryBinaries?.Count ?? 0) + (native?.Count ?? 0) + (atomics586?.Count ?? 0) + (counters596?.Count ?? 0) + (regex597?.Count ?? 0) + (binary598?.Count ?? 0) + (prepared807?.Count ?? 0)];
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
            if (prepared807 is not null)
                foreach (PreparedLoaderOwner807 backing in prepared807) retained[admitted++] = backing.Retain807(PreparedLoaderLeaseKind807.Guest);
            lease = new ProcessBinaryLease512(
                this,
                owner,
                backings is not null ? backings.Values.ToArray() : firstBacking490 is null ? [] : [firstBacking490],
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

        return RegisterLease161(
            lease,
            visits,
            canSummarize466 && retained.Length == 0 && lease.Handles.Length == 1 ? graph : null
        );
    }

    // Preserve first-seen handle identity and order; allocate the identity table
    // only when a second distinct real backing actually needs deduplication.
    private static void AddBacking490(
        ref BinaryPayloadHandle512? first,
        ref Dictionary<object, BinaryPayloadHandle512>? multiple,
        BinaryPayloadHandle512 handle
    )
    {
        if (multiple is not null)
        {
            multiple.TryAdd(handle.Identity, handle);

            return;
        }
        if (first is null)
        {
            first = handle;

            return;
        }
        if (ReferenceEquals(first.Identity, handle.Identity)) return;
        multiple = new Dictionary<object, BinaryPayloadHandle512>(ReferenceEqualityComparer.Instance)
        {
            { first.Identity, first },
            { handle.Identity, handle }
        };
    }

    // Caller qualified the exact immutable live subtree while holding H.
    // Only active registry metadata is read here: no heap/native/callback read.
    private BinaryPayloadHandle512? SnapshotActiveSummary468(object owner, Term graph)
    {
        lock (gate)
        {
            if (singleBackingSummaries466.TryGetValue(graph, out ProcessBinaryLease512? active)
                && ReferenceEquals(active.Owner, owner) && ReferenceEquals(active.CachedRoot466, graph)
                && leases.Contains(active) && active.Handles.Length == 1)
                return active.Handles[0];

            return null;
        }
    }

    private static bool CanSummarizeSingleBacking466(object owner, Term graph)
    {
        if (owner is not ProcessTermHeap506 heap || !Monitor.IsEntered(heap.Gate)
            || graph is not (Cons or TupleTerm)
            || ProcessTermHeap506.Storage(graph) is not ProcessTermSequence506 block
            || !ReferenceEquals(block.Owner, heap)) return false;

        // Exact immutable constructor proofs: published ordinary readonly
        // descendants only. Old pre-publication parents remain conservative.
        return heap.IsOwnedLocal(graph) && !block.IsMutableSequenceToken814
            && block.HasImmutableNativePublishedClosure441 && block.HasImmutableReadonlyCaptureClosure138
            && heap.CanUseImmutableClosure293(graph, block);
    }

    // Identical registration/rollback for proved-empty and walked graphs.
    // Even zero-edge roots own a REAL registry lease and one visit/operation.
    private ProcessBinaryLease512 RegisterLease161(ProcessBinaryLease512 lease, long visits, Term? summaryRoot466 = null)
    {
        try
        {
            lock (gate)
            {
                long nextVisits = checked(visitedTerms514 + visits);
                long nextOperations = checked(retainOperations514 + 1);
                long nextReferences = checked(liveBackingReferences514 + lease.Handles.Length);
                ProcessBinaryLease512? summaryHead495 = null;
                bool enroll466 = summaryRoot466 is not null && lease.Handles.Length == 1 && !lease.HasAdditionalRoots495
                    && (!singleBackingSummaries466.TryGetValue(summaryRoot466, out summaryHead495)
                        || ReferenceEquals(summaryHead495.Owner, lease.Owner) && leases.Contains(summaryHead495)
                            && ReferenceEquals(summaryHead495.SummaryRoot495, summaryRoot466));
                bool newSummary495 = enroll466 && summaryHead495 is null;
                // All capacity allocation and reference-hash lookup precede
                // publication of either active ownership registration.
                leases.EnsureCapacity(checked(leases.Count + 1));
                if (newSummary495) singleBackingSummaries466.EnsureCapacity(checked(singleBackingSummaries466.Count + 1));
                leases.Add(lease);
                if (enroll466) AddSummary495(lease, summaryRoot466!, summaryHead495);
                visitedTerms514 = nextVisits;
                retainOperations514 = nextOperations;
                liveBackingReferences514 = nextReferences;
                peakBackingReferences514 = Math.Max(peakBackingReferences514, nextReferences);
            }

            return lease;
        }
        catch
        {
            lock (gate)
            {
                leases.Remove(lease);
                RemoveSummary466(lease);
            }
            lease.Clear512();
            throw;
        }
    }

    // Caller owns the registry gate and has already reserved dictionary space.
    // Append to the oldest-first ring in O(1); every member is a real lease.
    private void AddSummary495(ProcessBinaryLease512 lease, Term root, ProcessBinaryLease512? head)
    {
        if (head is null)
        {
            singleBackingSummaries466.Add(root, lease);
            lease.SummaryRoot495 = root;
            lease.SummaryPrevious495 = lease;
            lease.SummaryNext495 = lease;
            lease.CachedRoot466 = root;

            return;
        }
        ProcessBinaryLease512 tail = head.SummaryPrevious495!;
        lease.SummaryRoot495 = root;
        lease.SummaryPrevious495 = tail;
        lease.SummaryNext495 = head;
        tail.SummaryNext495 = lease;
        head.SummaryPrevious495 = lease;
    }

    // No traversal or replacement search: select only the next already
    // registered, qualified active member. The last removal restores fallback.
    private void RemoveSummary466(ProcessBinaryLease512 lease)
    {
        Term? root = lease.SummaryRoot495;
        if (root is null)
        {
            lease.CachedRoot466 = null;

            return;
        }
        ProcessBinaryLease512 previous = lease.SummaryPrevious495!;
        ProcessBinaryLease512 next = lease.SummaryNext495!;
        if (ReferenceEquals(next, lease))
        {
            if (singleBackingSummaries466.TryGetValue(root, out ProcessBinaryLease512? only)
                && ReferenceEquals(only, lease)) singleBackingSummaries466.Remove(root);
        }
        else
        {
            previous.SummaryNext495 = next;
            next.SummaryPrevious495 = previous;
            if (singleBackingSummaries466.TryGetValue(root, out ProcessBinaryLease512? head)
                && ReferenceEquals(head, lease))
            {
                singleBackingSummaries466[root] = next;
                next.CachedRoot466 = root;
            }
        }
        lease.CachedRoot466 = null;
        lease.SummaryRoot495 = null;
        lease.SummaryPrevious495 = null;
        lease.SummaryNext495 = null;
    }

    internal void Release512(ProcessBinaryLease512 lease)
    {
        lock (gate)
        {
            if (!leases.Remove(lease))
                return;
            liveBackingReferences514 -= lease.Handles.Length;
            RemoveSummary466(lease);
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
