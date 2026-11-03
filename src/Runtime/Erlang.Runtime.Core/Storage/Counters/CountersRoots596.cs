namespace Erlang;

internal static class CountersRoots596
{
    internal static bool Contains596(Term source)
    {
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Counters421 })
                return true;
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    internal static CountersGuestRoot421? Direct596(Term value, ProcessRuntime? expected = null)
    {
        if (value is not ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Counters421 } reference)
            return null;
        CountersGuestRoot421 backing = reference.CountersBacking596
            ?? throw new InvalidOperationException(CountersOwnership596Names.Foreign);
        if (expected is not null && !ReferenceEquals(backing.Runtime596, expected))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        if (reference.OwnedStorage562 is { } storage)
        {
            if (storage is not ProcessTermSequence506 || storage.Owner is not ProcessTermHeap506 heap)
                throw new InvalidOperationException(MagicReference576Names.Invalid);

            return heap.ReadCountersMagic596(reference, backing);
        }

        return backing;
    }

    internal static IDisposable? RetainGraph596(
        Term source,
        ProcessRuntime? expected,
        bool skipOwned,
        bool host = false
    )
    {
        var roots = new HashSet<CountersGuestRoot421>(ReferenceEqualityComparer.Instance);
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (Direct596(value, expected) is { } root && (!skipOwned || !ProcessTermHeap506.HasOwnedMagicReference576(value)))
                roots.Add(root);
            traversal.EnqueueChildren(value);
        }
        if (roots.Count == 0)
            return null;
        var leases = new IDisposable[roots.Count];
        int count = 0;
        try
        {
            foreach (CountersGuestRoot421 root in roots)
                leases[count++] = host ? root.RetainHost596() : root.RetainGuest596();

            return new NativeResourceRootSet524(leases);
        }
        catch (Exception original)
        {
            Exception? cleanupFailure = null;
            for (int index = count - 1; index >= 0; index--)
            {
                try
                {
                    leases[index].Dispose();
                }
                catch (Exception failure)
                {
                    cleanupFailure ??= failure;
                }
            }
            if (cleanupFailure is not null)
                throw new AggregateException(original, cleanupFailure);
            throw;
        }
    }

    internal static ResourceReferenceTerm HostCopy596(ResourceReferenceTerm source, AtomicsHostCopyTransaction591? transaction591 = null)
    {
        CountersGuestRoot421 backing = Direct596(source)
            ?? throw new InvalidOperationException(CountersOwnership596Names.Foreign);
        var identity = new ReferenceTerm(source);
        transaction591?.Reserve591();
        CountersHostCustody596 lease = backing.RetainHost596();
        try
        {
            var result = new ResourceReferenceTerm(source.Entry, identity, lease);
            transaction591?.TrackCountersPrepared596(lease);

            return result;
        }
        catch (Exception original)
        {
            try
            {
                lease.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}
