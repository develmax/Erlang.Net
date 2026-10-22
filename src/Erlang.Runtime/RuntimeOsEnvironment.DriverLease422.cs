namespace Erlang;

internal sealed partial class RuntimeOsEnvironment
{
    private int driverLeases422;
    private bool driverStorageReleased422;

    internal RuntimeOsEnvironmentDriverLease422 AcquireDriverLease422()
    {
        lock (gate)
        {
            RequireOpen();
            var lease = new RuntimeOsEnvironmentDriverLease422(this);
            driverLeases422 = checked(driverLeases422 + 1);

            return lease;
        }
    }

    internal void ReleaseDriverLease422()
    {
        lock (gate)
        {
            if (driverLeases422 <= 0) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
            driverLeases422--;
            if (closed && driverLeases422 == 0) ReleaseDriverStorage422();
        }
    }

    private void RequireDriverLease422(RuntimeOsEnvironmentDriverLease422 lease)
    {
        if (!lease.IsOwnedBy(this) || driverStorageReleased422)
            throw new ObjectDisposedException(nameof(RuntimeOsEnvironmentDriverLease422));
    }

    internal string? ReadDriverLeased422(RuntimeOsEnvironmentDriverLease422 lease, byte[] bytes)
    {
        string? key = WindowsDriverAcp422.Decode(bytes);
        lock (gate)
        {
            RequireDriverLease422(lease);

            return key is null ? null : tree.Get(key);
        }
    }

    internal int WriteDriverLeased422(RuntimeOsEnvironmentDriverLease422 lease, byte[] keyBytes, byte[] valueBytes)
    {
        string? key = WindowsDriverAcp422.Decode(keyBytes), value = WindowsDriverAcp422.Decode(valueBytes);
        lock (gate)
        {
            RequireDriverLease422(lease);
            if (key is null || value is null) return -1;
            tree.Put(key, value);

            return 0;
        }
    }

    private void ReleaseDriverStorage422()
    {
        if (driverStorageReleased422) return;
        driverStorageReleased422 = true;
        tree.ClearDriverRoots422();
        locale.Dispose();
    }
}
