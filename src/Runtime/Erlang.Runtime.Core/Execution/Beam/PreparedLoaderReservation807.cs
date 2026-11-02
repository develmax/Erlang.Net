namespace Erlang;

internal sealed class PreparedLoaderReservation807 : IDisposable
{
    private PreparedLoaderLease807? lease;
    internal PreparedLoaderOwner807 Owner807 { get; }

    internal PreparedLoaderReservation807(PreparedLoaderOwner807 owner, PreparedLoaderLease807 lease)
    {
        if (lease.Kind807 != PreparedLoaderLeaseKind807.Operation || !lease.Owns807(owner))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        Owner807 = owner;
        this.lease = lease;
    }

    internal PreparedBeamImage Image807
        => Owner807.ReadImage807(Volatile.Read(ref lease) ?? throw new ObjectDisposedException(PreparedLoaderNames807.Closed));

    public void Dispose() => Interlocked.Exchange(ref lease, null)?.Dispose();
}
