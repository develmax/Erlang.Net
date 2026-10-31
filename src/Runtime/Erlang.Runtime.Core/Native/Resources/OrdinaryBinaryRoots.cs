namespace Erlang;

internal static class OrdinaryBinaryRoots
{
    internal static bool Contains(Term graph)
    {
        var walk = new ResourceRootTraversal595(graph);
        while (walk.TryPop(out Term? value))
        {
            if (value is BitString { ResourceBacking577.NativeStorage524: OrdinaryBinaryBacking }) return true;
            walk.EnqueueChildren(value);
        }

        return false;
    }

    internal static IDisposable? Retain(Term graph, ProcessRuntime? expected, bool skipOwnedProcessDescriptors = false)
    {
        var owners = new HashSet<OrdinaryBinaryBacking>(ReferenceEqualityComparer.Instance);
        var walk = new ResourceRootTraversal595(graph);
        while (walk.TryPop(out Term? value))
        {
            if (value is BitString { ResourceBacking577.NativeStorage524: OrdinaryBinaryBacking owner })
            {
                if (expected is not null && !ReferenceEquals(owner.Runtime, expected))
                    throw new NotSupportedException(NativeResource524Names.Foreign);
                if (!skipOwnedProcessDescriptors || ((BitString)value).OwnedStorage577 is null) owners.Add(owner);
            }
            walk.EnqueueChildren(value);
        }
        if (owners.Count == 0) return null;
        var leases = new IDisposable[owners.Count];
        int admitted = 0;
        try
        {
            foreach (var owner in owners)
            {
                owner.PinWritable();
                leases[admitted++] = owner.Retain();
            }

            return new NativeResourceRootSet524(leases);
        }
        catch (Exception original)
        {
            List<Exception>? failures = null;
            for (int index = 0; index < admitted; index++)
                try
                {
                    leases[index].Dispose();
                }
                catch (Exception cleanup)
                {
                    (failures ??= new()).Add(cleanup);
                }
            if (failures is not null) throw new AggregateException(new[] { original }.Concat(failures));
            throw;
        }
    }
}
