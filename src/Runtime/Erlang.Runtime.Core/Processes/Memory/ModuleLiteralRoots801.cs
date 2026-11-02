namespace Erlang;

// Independent metadata custody over actual module arena leases. Parent bundles
// contain no process frame/actor owner; their retention never delays sender Done.
internal sealed class ModuleLiteralRoots801 : IDisposable
{
    private readonly ModuleLiteralLease800[] leases;
    private readonly ModuleLiteralRoots801[] parents;
    private readonly RuntimeEmptyTupleLease803[] intrinsics803;
    private int references = 1;

    private ModuleLiteralRoots801(ModuleLiteralLease800[] leases, ModuleLiteralRoots801[] parents, RuntimeEmptyTupleLease803[]? intrinsics803 = null)
    {
        this.leases = leases;
        this.parents = parents;
        this.intrinsics803 = intrinsics803 ?? [];
    }

    // Transfer an actual registered native arena lease, never a Term/flag proof.
    internal static ModuleLiteralRoots801 TakeRegisteredLease357(ProcessRuntime runtime, ModuleLiteralLease800 lease)
    {
        if (!lease.Domain.IsRegisteredCaptureHolder134(runtime, lease))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);

        return new([lease], []);
    }

    internal ModuleLiteralRoots801 Retain801()
    {
        int previous = Volatile.Read(ref references);
        while (previous > 0)
        {
            int found = Interlocked.CompareExchange(ref references, checked(previous + 1), previous);
            if (found == previous) return this;
            previous = found;
        }
        throw new ObjectDisposedException(nameof(ModuleLiteralRoots801));
    }

    internal bool Owns801(ProcessTermSequence506 block)
    {
        if (Volatile.Read(ref references) <= 0) return false;
        foreach (ModuleLiteralLease800 lease in leases)
            if (lease.Domain.OwnsBlock800(block, lease)) return true;
        foreach (ModuleLiteralRoots801 parent in parents)
            if (parent.Owns801(block)) return true;

        return false;
    }

    internal bool OwnsValue803(Term value)
        => ProcessTermHeap506.Storage(value) switch
        {
            ProcessTermSequence506 block => Owns801(block),
            RuntimeEmptyTupleStorage803 empty => empty.Domain803.IsCanonical803(value) && OwnsIntrinsic803(empty),
            _ => false
        };

    private bool OwnsIntrinsic803(RuntimeEmptyTupleStorage803 empty)
    {
        if (Volatile.Read(ref references) <= 0) return false;
        foreach (RuntimeEmptyTupleLease803 lease in intrinsics803)
            if (lease.Domain.Owns803(empty, lease)) return true;
        foreach (ModuleLiteralRoots801 parent in parents)
            if (parent.OwnsIntrinsic803(empty)) return true;

        return false;
    }

    private ModuleLiteralRoots801? FindIntrinsicLeaf803(RuntimeEmptyTupleStorage803 empty)
    {
        if (Volatile.Read(ref references) <= 0) return null;
        foreach (RuntimeEmptyTupleLease803 lease in intrinsics803)
            if (lease.Domain.Owns803(empty, lease)) return this;
        foreach (ModuleLiteralRoots801 parent in parents)
            if (parent.FindIntrinsicLeaf803(empty) is { } found) return found;

        return null;
    }

    private bool TryFork801(Term root, out ModuleLiteralLease800? derived)
    {
        derived = null;
        if (Volatile.Read(ref references) <= 0) return false;
        foreach (ModuleLiteralLease800 lease in leases)
            if (lease.TryFork(root, out derived)) return true;
        foreach (ModuleLiteralRoots801 parent in parents)
            if (parent.TryFork801(root, out derived)) return true;

        return false;
    }

    private ModuleLiteralRoots801? FindLeafOwner801(ProcessTermSequence506 block)
    {
        if (Volatile.Read(ref references) <= 0) return null;
        foreach (ModuleLiteralLease800 lease in leases)
            if (lease.Domain.OwnsBlock800(block, lease)) return this;
        foreach (ModuleLiteralRoots801 parent in parents)
            if (parent.FindLeafOwner801(block) is { } found) return found;

        return null;
    }

    // H stabilizes the actual holder. This only retains an authenticated leaf;
    // no new module arena lease or source image admission is acquired here.
    internal ModuleLiteralRoots801? RetainMatchingLeaf129(ProcessTermSequence506? block, RuntimeEmptyTupleStorage803? empty)
        => (block is not null ? FindLeafOwner801(block)
            : empty is not null ? FindIntrinsicLeaf803(empty) : null)?.Retain801();

    internal static ModuleLiteralRoots801? RetainCombined801(IEnumerable<ModuleLiteralRoots801?> sources)
    {
        var leaves = new HashSet<ModuleLiteralRoots801>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<ModuleLiteralRoots801>(sources.OfType<ModuleLiteralRoots801>());
        while (pending.TryPop(out ModuleLiteralRoots801? root))
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref root.references) <= 0, root);
            if (root.leases.Length != 0 || root.intrinsics803.Length != 0) leaves.Add(root);
            foreach (ModuleLiteralRoots801 parent in root.parents) pending.Push(parent);
        }
        if (leaves.Count == 0) return null;
        var retained = new List<ModuleLiteralRoots801>(leaves.Count);
        try
        {
            foreach (ModuleLiteralRoots801 leaf in leaves) retained.Add(leaf.Retain801());

            return new([], retained.ToArray());
        }
        catch (Exception primary)
        {
            Exception failure = primary;
            foreach (ModuleLiteralRoots801 root in retained)
                try
                {
                    root.Dispose();
                }
                catch (Exception cleanup)
                {
                    failure = new AggregateException(failure, cleanup);
                }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }

    private static bool TryCaptureFlat222(
        ProcessTermHeap506 heap,
        ModuleLiteralRoots801 sidecar,
        HashSet<ModuleLiteralRoots801> borrowed,
        HashSet<ModuleLiteralDomain800> domains,
        HashSet<RuntimeEmptyTupleDomain803> intrinsicDomains
    )
    {
        foreach (ModuleLiteralRoots801 parent in sidecar.parents)
            if (parent.parents.Length != 0) return false;
        // Authenticate EVERY actual node/lease before even domain membership
        // checks. Holding the genuine sidecar stabilizes its immutable parents.
        AuthenticateFlatNode222(heap, sidecar);
        foreach (ModuleLiteralRoots801 parent in sidecar.parents)
            AuthenticateFlatNode222(heap, parent);
        // Match DFS postorder: each leaf parent precedes the mixed root.
        foreach (ModuleLiteralRoots801 parent in sidecar.parents)
            ContributeFlatNode222(
                parent,
                borrowed,
                domains,
                intrinsicDomains
            );
        ContributeFlatNode222(
            sidecar,
            borrowed,
            domains,
            intrinsicDomains
        );

        return true;
    }

    private static void AuthenticateFlatNode222(ProcessTermHeap506 heap, ModuleLiteralRoots801 node)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref node.references) <= 0, node);
        foreach (ModuleLiteralLease800 lease in node.leases)
            if (!lease.Domain.IsRegisteredCaptureHolder134(heap.Runtime524, lease))
                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        foreach (RuntimeEmptyTupleLease803 lease in node.intrinsics803)
            if (!lease.Domain.IsRegisteredCaptureHolder134(heap.Runtime524, lease))
                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
    }

    private static void ContributeFlatNode222(
        ModuleLiteralRoots801 node,
        HashSet<ModuleLiteralRoots801> borrowed,
        HashSet<ModuleLiteralDomain800> domains,
        HashSet<RuntimeEmptyTupleDomain803> intrinsicDomains
    )
    {
        if (node.leases.Length == 0 && node.intrinsics803.Length == 0) return;
        bool contributes = false;
        foreach (ModuleLiteralLease800 lease in node.leases)
            if (!domains.Contains(lease.Domain)) contributes = true;
        foreach (RuntimeEmptyTupleLease803 lease in node.intrinsics803)
            if (!intrinsicDomains.Contains(lease.Domain)) contributes = true;
        if (!contributes) return;
        if (borrowed.Add(node))
        {
            try
            {
                node.Retain801();
            }
            catch
            {
                borrowed.Remove(node);
                throw;
            }
        }
        // A selected mixed owner also covers its authenticated leaf parents.
        // Do not let a later same-domain sibling build another owner chain.
        foreach (ModuleLiteralLease800 lease in node.leases) domains.Add(lease.Domain);
        foreach (RuntimeEmptyTupleLease803 lease in node.intrinsics803) intrinsicDomains.Add(lease.Domain);
        foreach (ModuleLiteralRoots801 parent in node.parents)
        {
            foreach (ModuleLiteralLease800 lease in parent.leases) domains.Add(lease.Domain);
            foreach (RuntimeEmptyTupleLease803 lease in parent.intrinsics803) intrinsicDomains.Add(lease.Domain);
        }
    }

    private static bool CaptureCertifiedBlock134(
        ProcessTermHeap506 heap,
        Term value,
        HashSet<ModuleLiteralRoots801> borrowed,
        HashSet<ModuleLiteralDomain800> domains,
        HashSet<RuntimeEmptyTupleDomain803> intrinsicDomains
    )
    {
        ModuleLiteralRoots801? sidecar = heap.RetainCertifiedLiteralBlock134(value);
        if (sidecar is null) return false;
        Exception? failure = null;
        try
        {
            if (!TryCaptureFlat222(
                heap,
                sidecar,
                borrowed,
                domains,
                intrinsicDomains
            ))
            {
                var nodes = new List<ModuleLiteralRoots801>();
                var seen = new HashSet<ModuleLiteralRoots801>(ReferenceEqualityComparer.Instance);
                var traversal = new Stack<(ModuleLiteralRoots801 Node, bool Expanded)>();
                var pending = new Stack<ModuleLiteralRoots801>();
                traversal.Push((sidecar, false));
                // True DFS postorder: complete all actual parents before a mixed
                // lease owner. Reverse preorder is not sufficient for a shared DAG.
                // Authenticate EVERY actual lease before ANY domain deduplication.
                while (traversal.TryPop(out var entry))
                {
                    ModuleLiteralRoots801 node = entry.Node;
                    if (entry.Expanded)
                    {
                        nodes.Add(node);
                        continue;
                    }
                    if (!seen.Add(node)) continue;
                    ObjectDisposedException.ThrowIf(Volatile.Read(ref node.references) <= 0, node);
                    foreach (ModuleLiteralLease800 lease in node.leases)
                        if (!lease.Domain.IsRegisteredCaptureHolder134(heap.Runtime524, lease))
                            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
                    foreach (RuntimeEmptyTupleLease803 lease in node.intrinsics803)
                        if (!lease.Domain.IsRegisteredCaptureHolder134(heap.Runtime524, lease))
                            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
                    traversal.Push((node, true));
                    for (int index = node.parents.Length - 1; index >= 0; index--)
                        traversal.Push((node.parents[index], false));
                }
                foreach (ModuleLiteralRoots801 leaf in nodes)
                {
                    // Pure aggregate nodes never become another borrowed parent;
                    // choose actual lease-holding leaves, not per-block bundles.
                    if (leaf.leases.Length == 0 && leaf.intrinsics803.Length == 0) continue;
                    bool contributes = false;
                    foreach (ModuleLiteralLease800 lease in leaf.leases)
                        if (!domains.Contains(lease.Domain)) contributes = true;
                    foreach (RuntimeEmptyTupleLease803 lease in leaf.intrinsics803)
                        if (!intrinsicDomains.Contains(lease.Domain)) contributes = true;
                    if (!contributes) continue;
                    if (borrowed.Add(leaf))
                    {
                        try
                        {
                            leaf.Retain801();
                        }
                        catch
                        {
                            borrowed.Remove(leaf);
                            throw;
                        }
                    }
                    // A real leaf may itself hold immutable parents. Record ALL of
                    // that selected owner's authenticated domains, so same-domain
                    // siblings are not retained into a linear chain on later Cons.
                    seen.Clear();
                    pending.Push(leaf);
                    while (pending.TryPop(out ModuleLiteralRoots801? owned))
                    {
                        if (!seen.Add(owned)) continue;
                        foreach (ModuleLiteralLease800 lease in owned.leases) domains.Add(lease.Domain);
                        foreach (RuntimeEmptyTupleLease803 lease in owned.intrinsics803) intrinsicDomains.Add(lease.Domain);
                        foreach (ModuleLiteralRoots801 parent in owned.parents) pending.Push(parent);
                    }
                }
            }
        }
        catch (Exception error)
        {
            failure = error;
        }
        try
        {
            sidecar.Dispose();
        }
        catch (Exception cleanup)
        {
            failure = failure is null ? cleanup : new AggregateException(failure, cleanup);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();

        return true;
    }

    internal static ModuleLiteralRoots801? Capture801(ProcessTermHeap506 heap, Term graph, ModuleLiteralRoots801? carried = null)
    {
        bool underHeap = Monitor.IsEntered(heap.Gate);
        if (ImmediateTermRoot161.IsEmpty(graph)) return null;
        // A binary without a resource-slice origin has no literal children.
        // Keep native sequence authentication before skipping the empty walk;
        // literal-arena and foreign storage still use full custody handling.
        if (graph is BitString { ResourceSliceOrigin577: null })
        {
            if (ProcessTermHeap506.Storage(graph) is not ProcessTermSequence506 binaryBlock489)
                return null;
            if (ReferenceEquals(binaryBlock489.Owner, heap) && heap.LiteralDomain800 is null)
            {
                lock (heap.Gate)
                {
                    _ = heap.IsOwnedLocal(graph);

                    return null;
                }
            }
        }
        // The same successful constructor capture proof used by the walk below.
        // Admit only live local process Cons/Tuple storage before allocating
        // traversal collections; literal arenas and every other family fall back.
        if (graph is Cons or TupleTerm && heap.LiteralDomain800 is null
            && ProcessTermHeap506.Storage(graph) is ProcessTermSequence506 emptyBlock482
            && ReferenceEquals(emptyBlock482.Owner, heap))
        {
            lock (heap.Gate)
            {
                if (heap.IsOwnedLocal(graph)
                    && heap.CanUseImmutableClosure293(graph, emptyBlock482)
                    && emptyBlock482.HasEmptyLiteralCapture200)
                    return null;
            }
        }
        // For one arena root under H, retain the same actual matching leaf
        // the fallback would borrow into an aggregate. No arena lease is minted.
        if (underHeap && carried is null
            && ProcessTermHeap506.Storage(graph) is ProcessTermSequence506 literalBlock486
            && literalBlock486.Owner is ProcessTermHeap506 literalHeap486
            && literalHeap486.LiteralDomain800 is { IsSealed800: true } literalDomain486)
        {
            if (!ReferenceEquals(literalDomain486.Runtime800, heap.Runtime524))
                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
            ModuleLiteralRoots801? literalOwner487 = null;
            try
            {
                // Preserve preferred pending-holder authentication, overflow and
                // cleanup even when another actual frame/block holds the root.
                using ModuleLiteralRoots801? preferred487 = heap.PendingLiteralRoots801?.Retain801();
                literalOwner487 = heap.RetainMatchingLiteralAuthority129(literalBlock486);
            }
            catch (Exception primary)
            {
                try
                {
                    literalOwner487?.Dispose();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                throw;
            }
            if (literalOwner487 is not null) return literalOwner487;
        }
        List<ModuleLiteralRoots801>? matchedSources = null;
        ModuleLiteralRoots801[] preferredSources = [];
        bool preferredLoaded = false;

        // Actual retained holders only. Graphs without shared literals never
        // enumerate every frame/block authority in the process heap.
        void EnsurePreferred()
        {
            if (preferredLoaded) return;
            preferredSources = heap.SnapshotPreferredLiteralAuthorities127(carried);
            preferredLoaded = true;
        }

        ModuleLiteralRoots801? KeepMatchedSource(ModuleLiteralRoots801? retained)
        {
            if (retained is null) return null;
            try
            {
                (matchedSources ??= new()).Add(retained);

                return retained;
            }
            catch (Exception primary)
            {
                try
                {
                    retained.Dispose();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                throw;
            }
        }

        ModuleLiteralRoots801? FindSource(ProcessTermSequence506 block)
        {
            EnsurePreferred();
            foreach (ModuleLiteralRoots801 candidate in preferredSources)
                if (candidate.FindLeafOwner801(block) is { } found) return found;
            if (matchedSources is not null)
                foreach (ModuleLiteralRoots801 candidate in matchedSources)
                    if (candidate.FindLeafOwner801(block) is { } found) return found;

            return null;
        }
        var next = new List<ModuleLiteralLease800>();
        var nextIntrinsics = new List<RuntimeEmptyTupleLease803>();
        var borrowed = new HashSet<ModuleLiteralRoots801>(ReferenceEqualityComparer.Instance);
        var domains = new HashSet<ModuleLiteralDomain800>(ReferenceEqualityComparer.Instance);
        var intrinsicDomains134 = new HashSet<RuntimeEmptyTupleDomain803>(ReferenceEqualityComparer.Instance);
        ModuleLiteralRoots801? result = null;
        Exception? failure = null;
        try
        {
            var pending = new Stack<Term>();
            var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
            pending.Push(graph);
            while (pending.TryPop(out Term? current))
            {
                if (!seen.Add(current)) continue;
                if (ProcessTermHeap506.Storage(current) is RuntimeEmptyTupleStorage803 empty)
                {
                    _ = heap.IsAuthenticatedEmptyTuple803(current);
                    if (intrinsicDomains134.Contains(empty.Domain803)) continue;
                    EnsurePreferred();
                    ModuleLiteralRoots801? intrinsicSource = null;
                    foreach (ModuleLiteralRoots801 source in preferredSources)
                    {
                        intrinsicSource = source.FindIntrinsicLeaf803(empty);
                        if (intrinsicSource is not null) break;
                    }
                    if (intrinsicSource is null)
                    {
                        // Existing intrinsic holders can outlive owner admission.
                        // Preserve that fallback before attempting a fresh lease.
                        if (matchedSources is not null)
                            foreach (ModuleLiteralRoots801 source in matchedSources)
                            {
                                intrinsicSource = source.FindIntrinsicLeaf803(empty);
                                if (intrinsicSource is not null) break;
                            }
                        intrinsicSource ??= KeepMatchedSource(heap.RetainMatchingIntrinsicAuthority129(empty));
                    }
                    if (intrinsicSource is not null)
                    {
                        if (borrowed.Add(intrinsicSource))
                        {
                            try
                            {
                                intrinsicSource.Retain801();
                            }
                            catch
                            {
                                borrowed.Remove(intrinsicSource);
                                throw;
                            }
                        }
                    }
                    else
                    {
                        RuntimeEmptyTupleLease803 intrinsic = empty.Domain803.Acquire803(empty);
                        try
                        {
                            nextIntrinsics.Add(intrinsic);
                        }
                        catch
                        {
                            intrinsic.Dispose();
                            throw;
                        }
                    }
                    intrinsicDomains134.Add(empty.Domain803);
                    continue;
                }
                if (ProcessTermHeap506.Storage(current) is ProcessTermSequence506 block
                    && block.Owner is ProcessTermHeap506 storageHeap)
                {
                    if (storageHeap.LiteralDomain800 is { IsSealed800: true } domain)
                    {
                        if (!ReferenceEquals(domain.Runtime800, heap.Runtime524))
                            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
                        if (!domains.Add(domain)) continue;
                        ModuleLiteralRoots801? source = FindSource(block);
                        if (underHeap)
                        {
                            if (source is null)
                                source = KeepMatchedSource(heap.RetainMatchingLiteralAuthority129(block));
                            // No new arena lease acquisition while H is held.
                            // Derive only from an already retained actual holder.
                            if (source is null) throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
                            if (borrowed.Add(source))
                            {
                                try
                                {
                                    source.Retain801();
                                }
                                catch
                                {
                                    borrowed.Remove(source);
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            ModuleLiteralLease800? derived = null;
                            if (source?.TryFork801(current, out derived) != true)
                                derived = RuntimeActorExecutionTurn.CurrentLiteralContext801(heap.Runtime524)?.ForkExecutingModuleLiteral800(current);
                            if (derived is null)
                            {
                                source = KeepMatchedSource(heap.RetainMatchingLiteralAuthority129(block));
                                _ = source?.TryFork801(current, out derived);
                            }
                            if (derived is null || !domain.OwnsBlock800(block, derived))
                            {
                                derived?.Dispose();
                                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
                            }
                            try
                            {
                                next.Add(derived);
                            }
                            catch
                            {
                                derived.Dispose();
                                throw;
                            }
                        }
                        continue; // The sealed arena owns its immutable closure.
                    }
                    if (!ReferenceEquals(storageHeap, heap))
                        throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
                    bool immutable293;
                    lock (heap.Gate)
                    {
                        _ = heap.IsOwnedLocal(current);
                        immutable293 = heap.CanUseImmutableClosure293(current, block);
                    }
                    // This constructor proof comes from a successful ACTUAL
                    // empty capture, not from absence of a sidecar/cache row.
                    if (immutable293 && block.HasEmptyLiteralCapture200) continue;
                    if (immutable293
                        && CaptureCertifiedBlock134(
                            heap,
                            current,
                            borrowed,
                            domains,
                            intrinsicDomains134
                        )) continue;
                    if (immutable293
                        && (block.HasImmutablePlainClosure550 || block.HasImmutableNoBackingClosure192)) continue;
                }
                foreach (Term child in NativeMapOwnedRoots510.Children(current)) pending.Push(child);
            }
            if (next.Count != 0 || borrowed.Count != 0 || nextIntrinsics.Count != 0)
            {
                result = new ModuleLiteralRoots801(next.ToArray(), borrowed.ToArray(), nextIntrinsics.ToArray());
                next.Clear();
                nextIntrinsics.Clear();
                borrowed.Clear();
            }
        }
        catch (Exception error)
        {
            failure = error;
        }
        foreach (IDisposable item in next.Cast<IDisposable>().Concat(nextIntrinsics).Concat(borrowed).Concat(matchedSources ?? Enumerable.Empty<ModuleLiteralRoots801>()).Concat(preferredSources))
            try
            {
                item.Dispose();
            }
            catch (Exception error)
            {
                failure = failure is null ? error : new AggregateException(failure, error);
            }
        if (failure is null) return result;
        try
        {
            result?.Dispose();
        }
        catch (Exception error)
        {
            failure = new AggregateException(failure, error);
        }
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
    }

    public void Dispose()
    {
        int left = Interlocked.Decrement(ref references);
        if (left < 0) throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
        if (left != 0) return;
        List<Exception>? failures = null;
        foreach (IDisposable item in leases.Cast<IDisposable>().Concat(intrinsics803).Concat(parents))
            try
            {
                item.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        if (failures is not null) throw new AggregateException(failures);
    }
}
