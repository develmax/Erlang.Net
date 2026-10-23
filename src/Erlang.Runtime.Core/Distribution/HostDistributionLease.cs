namespace Erlang;

public sealed class HostDistributionLease : IDisposable
{
    private HostDistributionHandle? handle;

    internal HostDistributionLease(HostDistributionHandle handle) => this.handle = handle;

    public HostDistributionSnapshot Snapshot()
    {
        var current = Volatile.Read(ref handle) ?? throw new ObjectDisposedException(nameof(HostDistributionLease));

        return current.Owner.Snapshot(current);
    }

    public void Dispose() => Interlocked.Exchange(ref handle, null);
}
