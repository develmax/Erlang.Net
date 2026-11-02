namespace Erlang;

internal sealed class PreparedLoaderHost807 : IDisposable
{
    private PreparedLoaderLease807? lease;

    internal PreparedLoaderHost807(PreparedLoaderOwner807 owner)
        => lease = owner.Retain807(PreparedLoaderLeaseKind807.Host);

    internal bool Owns807(PreparedLoaderOwner807 owner) => Volatile.Read(ref lease)?.Owns807(owner) == true;

    internal PreparedBeamImage ReadImage807(PreparedLoaderOwner807 owner)
        => owner.ReadImage807(Volatile.Read(ref lease) ?? throw new ObjectDisposedException(PreparedLoaderNames807.Closed));

    public void Dispose()
    {
        Interlocked.Exchange(ref lease, null)?.Dispose();
        GC.SuppressFinalize(this);
    }

    ~PreparedLoaderHost807()
    {
        // Secondary Host adapter cleanup only. Guest lifetimes are owned by
        // physical headers/graph leases and never wait for this finalizer.
        try
        {
            Dispose();
        }
        catch { /* The actual owner records asynchronous cleanup failures. */ }
    }
}
