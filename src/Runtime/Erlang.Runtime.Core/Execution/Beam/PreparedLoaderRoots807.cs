namespace Erlang;

internal static class PreparedLoaderRoots807
{
    internal static PreparedLoaderOwner807? Direct807(Term value, ProcessRuntime? expected = null)
    {
        if (value is not ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.PreparedBeam } reference) return null;
        PreparedLoaderOwner807 owner = reference.PreparedBacking807 ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        if (expected is not null && !ReferenceEquals(owner.Runtime807, expected))
            throw new NotSupportedException(NativeResource524Names.Foreign);
        if (reference.PreparedHost807 is { } host && !host.Owns807(owner))
            throw new ObjectDisposedException(PreparedLoaderNames807.Closed);
        if (reference.OwnedStorage562 is { } storage)
        {
            if (storage is not ProcessTermSequence506 || storage.Owner is not ProcessTermHeap506 heap)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);

            return heap.ReadPreparedMagic807(reference, owner);
        }

        return owner;
    }

    internal static bool Contains807(Term source)
    {
        if (!PreparedLoaderOwner807.AnyLive807) return false;
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (value is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.PreparedBeam }) return true;
            traversal.EnqueueChildren(value);
        }

        return false;
    }

    internal static IDisposable? RetainGraph807(
        Term source,
        ProcessRuntime? expected,
        bool skipOwned,
        bool host
    )
    {
        if (!PreparedLoaderOwner807.AnyLive807) return null;
        var roots = new HashSet<PreparedLoaderOwner807>(ReferenceEqualityComparer.Instance);
        var traversal = new ResourceRootTraversal595(source);
        while (traversal.TryPop(out Term? value))
        {
            if (Direct807(value, expected) is { } owner && (!skipOwned || !ProcessTermHeap506.HasOwnedMagicReference576(value)))
                roots.Add(owner);
            traversal.EnqueueChildren(value);
        }
        if (roots.Count == 0) return null;
        var leases = new IDisposable[roots.Count];
        var failures = new List<Exception>(checked(roots.Count + 1));
        int count = 0;
        try
        {
            foreach (PreparedLoaderOwner807 owner in roots)
                leases[count++] = owner.Retain807(host ? PreparedLoaderLeaseKind807.Host : PreparedLoaderLeaseKind807.Guest);

            return new NativeResourceRootSet524(leases);
        }
        catch (Exception primary)
        {
            failures.Add(primary);
            while (count != 0)
                try
                {
                    leases[--count].Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (failures.Count > 1) throw new AggregateException(failures);
            throw;
        }
    }

    internal static ResourceReferenceTerm HostCopy807(ResourceReferenceTerm source, AtomicsHostCopyTransaction591? transaction = null)
    {
        PreparedLoaderOwner807 owner = Direct807(source) ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        var identity = new ReferenceTerm(source);
        transaction?.Reserve591();
        var host = new PreparedLoaderHost807(owner);
        try
        {
            var result = new ResourceReferenceTerm(source.Entry, identity, host);
            transaction?.TrackPreparedLoader807(host);

            return result;
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }
}
