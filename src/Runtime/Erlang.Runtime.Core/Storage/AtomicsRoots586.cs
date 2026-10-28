namespace Erlang;

internal static class AtomicsRoots586
{
    internal static bool Contains586(Term source)
    {
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Atomics413 })
                return true;
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    internal static AtomicsGuestRoot413? Direct586(Term value, ProcessRuntime? expected = null)
    {
        if (value is not ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Atomics413 } reference)
            return null;
        AtomicsGuestRoot413 backing = reference.AtomicsBacking586
            ?? throw new InvalidOperationException(AtomicsOwnership586Names.Foreign);
        if (expected is not null && !ReferenceEquals(backing.Runtime586, expected))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        if (reference.OwnedStorage562 is { } storage)
        {
            if (storage is not ProcessTermSequence506 || storage.Owner is not ProcessTermHeap506 heap)
                throw new InvalidOperationException(MagicReference576Names.Invalid);

            return heap.ReadAtomicsMagic586(reference, backing);
        }

        return backing;
    }

    internal static IDisposable? RetainGraph586(
        Term source,
        ProcessRuntime? expected,
        bool skipOwned,
        bool host = false
    )
    {
        var roots = new HashSet<AtomicsGuestRoot413>(ReferenceEqualityComparer.Instance);
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (Direct586(value, expected) is { } root && (!skipOwned || !ProcessTermHeap506.HasOwnedMagicReference576(value)))
                roots.Add(root);
            traversal.EnqueueChildren(value);
        }
        if (roots.Count == 0)
            return null;
        var leases = new IDisposable[roots.Count];
        int count = 0;
        try
        {
            foreach (AtomicsGuestRoot413 root in roots)
                leases[count++] = host ? root.RetainHost586() : root.RetainGuest586();

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

    internal static ResourceReferenceTerm HostCopy586(ResourceReferenceTerm source, AtomicsHostCopyTransaction591? transaction591 = null)
    {
        AtomicsGuestRoot413 backing = Direct586(source)
            ?? throw new InvalidOperationException(AtomicsOwnership586Names.Foreign);
        var identity = new ReferenceTerm(source);
        transaction591?.Reserve591();
        AtomicsHostCustody586 lease = backing.RetainHost586();
        try
        {
            var result = new ResourceReferenceTerm(source.Entry, identity, lease);
            transaction591?.TrackPrepared591(lease);

            return result;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }
}
