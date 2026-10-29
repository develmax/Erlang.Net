namespace Erlang;

internal static class ReMagicRoots597
{
    internal static ReMagicBacking597? Direct597(Term value, ProcessRuntime? expected = null)
    {
        if (value is not ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Regex411 } reference) return null;
        ReMagicBacking597 backing = reference.RegexBacking597 ?? throw new InvalidOperationException(ReMagicNames597.Invalid);
        if (expected is not null && !ReferenceEquals(backing.Runtime597, expected))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        if (reference.OwnedStorage562 is { } storage)
        {
            if (storage is not ProcessTermSequence506 || storage.Owner is not ProcessTermHeap506 heap)
                throw new InvalidOperationException(ReMagicNames597.Invalid);

            return heap.ReadRegexMagic597(reference, backing);
        }

        return backing;
    }

    internal static bool Contains597(Term source)
    {
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Regex411 }) return true;
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    internal static IDisposable? RetainGraph597(
        Term source,
        ProcessRuntime? expected,
        bool skipOwned,
        bool host
    )
    {
        var roots = new HashSet<ReMagicBacking597>(ReferenceEqualityComparer.Instance);
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (Direct597(value, expected) is { } backing && (!skipOwned || !ProcessTermHeap506.HasOwnedMagicReference576(value)))
                roots.Add(backing);
            traversal.EnqueueChildren(value);
        }
        if (roots.Count == 0) return null;
        var leases = new IDisposable[roots.Count];
        int count = 0;
        try
        {
            foreach (var backing in roots)
                leases[count++] = backing.Retain(host ? ReMagicLeaseKind597.Host : ReMagicLeaseKind597.Guest);

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

    internal static ResourceReferenceTerm HostCopy597(ResourceReferenceTerm source, AtomicsHostCopyTransaction591? transaction = null)
    {
        ReMagicBacking597 backing = Direct597(source) ?? throw new InvalidOperationException(ReMagicNames597.Invalid);
        var identity = new ReferenceTerm(source);
        transaction?.Reserve591();
        var lease = new ReMagicHost597(backing);
        try
        {
            var result = new ResourceReferenceTerm(source.Entry, identity, lease);
            transaction?.TrackRegex597(lease);

            return result;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }
}
