namespace Erlang;

public sealed class DistributionPendingLease : IDisposable
{
    private ReferenceResourceLease<RuntimeDistributionPendingEntry>? lease;
    private readonly uint connectionId;

    internal DistributionPendingLease(ReferenceResourceLease<RuntimeDistributionPendingEntry> lease, uint connectionId)
    {
        this.lease = lease;
        this.connectionId = connectionId;
    }

    public DistributionPendingSnapshot Snapshot()
    {
        var current = Volatile.Read(ref lease) ?? throw new ObjectDisposedException(DistributionPendingDiagnostics.Lease);
        var entry = current.Value;

        return entry.Owner.SnapshotEntry(entry, connectionId);
    }

    public void Dispose() => Interlocked.Exchange(ref lease, null)?.Dispose();
}
