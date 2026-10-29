namespace Erlang;

internal static class BinaryProgramRoots598
{
    internal static BinaryProgramBacking598? Direct598(Term value, ProcessRuntime? expected = null)
    {
        if (value is not ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.BinarySearchPattern } reference) return null;
        BinaryProgramBacking598 backing = reference.BinaryBacking598 ?? throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        if (expected is not null && !ReferenceEquals(backing.Runtime598, expected))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        if (reference.OwnedStorage562 is { } storage)
        {
            if (storage is not ProcessTermSequence506 || storage.Owner is not ProcessTermHeap506 heap)
                throw new InvalidOperationException(BinaryArenaNames598.Invalid);

            return heap.ReadBinaryMagic598(reference, backing);
        }

        return backing;
    }

    internal static bool Contains598(Term source)
    {
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.BinarySearchPattern }) return true;
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    internal static IDisposable? RetainGraph598(
        Term source,
        ProcessRuntime? expected,
        bool skipOwned,
        bool host
    )
    {
        var roots = new HashSet<BinaryProgramBacking598>(ReferenceEqualityComparer.Instance);
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (Direct598(value, expected) is { } backing && (!skipOwned || !ProcessTermHeap506.HasOwnedMagicReference576(value)))
                roots.Add(backing);
            traversal.EnqueueChildren(value);
        }
        if (roots.Count == 0) return null;
        var leases = new IDisposable[roots.Count];
        int count = 0;
        try
        {
            foreach (var backing in roots)
                leases[count++] = backing.Retain598(host ? BinaryProgramLeaseKind598.Host : BinaryProgramLeaseKind598.Guest);

            return new NativeResourceRootSet524(leases);
        }
        catch (Exception original)
        {
            Exception? cleanup = null;
            while (count != 0)
                try
                {
                    leases[--count].Dispose();
                }
                catch (Exception error)
                {
                    cleanup ??= error;
                }
            if (cleanup is not null) throw new AggregateException(original, cleanup);
            throw;
        }
    }

    internal static ResourceReferenceTerm HostCopy598(ResourceReferenceTerm source, AtomicsHostCopyTransaction591? transaction = null)
    {
        BinaryProgramBacking598 backing = Direct598(source) ?? throw new InvalidOperationException(BinaryArenaNames598.Invalid);
        var identity = new ReferenceTerm(source);
        transaction?.Reserve591();
        var lease = new BinaryProgramHost598(backing);
        try
        {
            var result = new ResourceReferenceTerm(source.Entry, identity, lease);
            transaction?.TrackBinary598(lease);

            return result;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }
}
