namespace Erlang;

// CLR finalization is authority for an escaped HOST wrapper only, never guest GC.
internal sealed class ReMagicHost597 : IDisposable
{
    private ReMagicLease597? lease;

    internal ReMagicHost597(ReMagicBacking597 backing) => lease = backing.Retain(ReMagicLeaseKind597.Host);

    internal bool Owns(ReMagicBacking597 backing) => Volatile.Read(ref lease)?.Owns(backing) == true;

    public void Dispose()
    {
        Interlocked.Exchange(ref lease, null)?.Dispose();
        GC.SuppressFinalize(this);
    }

    ~ReMagicHost597()
    {
        try
        {
            Dispose();
        }
        catch { /* Owner/global retirement retains and reports failed custody. */ }
    }
}
