namespace Erlang;

// Public reservation retains an explicit holder. Ordinary host exports use a
// lazy rollback scope and commit directly to their returned wrapper carriers.
internal sealed class AtomicsHostCopyTransaction591 : IDisposable
{
    private readonly List<IDisposable> leases = new();
    private readonly List<Exception> failures = new();
    private bool transferred;

    internal void Reserve591()
    {
        leases.EnsureCapacity(checked(leases.Count + 1));
        failures.EnsureCapacity(checked(leases.Count + 1));
    }

    internal void TrackPrepared591(AtomicsHostCustody586 lease) => leases.Add(lease);

    internal void TrackCountersPrepared596(CountersHostCustody596 lease) => leases.Add(lease);

    internal void TrackRegex597(ReMagicHost597 lease) => leases.Add(lease);

    internal IDisposable Commit591()
    {
        var result = new NativeResourceRootSet524(leases.Cast<IDisposable>().ToArray());
        transferred = true;

        return result;
    }

    internal void CommitWrappers593()
    {
        // Each returned wrapper already owns its exact lease. Do not allocate
        // an extra permanent holder or change the number of backing roots.
        transferred = true;
        leases.Clear();
        failures.Clear();
    }

    public void Dispose()
    {
        if (transferred) return;
        // Release every acquired carrier before allocating error aggregation.
        foreach (var lease in leases)
            try
            {
                lease.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        leases.Clear();
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
