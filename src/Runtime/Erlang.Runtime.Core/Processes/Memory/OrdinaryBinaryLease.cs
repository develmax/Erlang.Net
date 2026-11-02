namespace Erlang;

internal sealed class OrdinaryBinaryLease(OrdinaryBinaryBacking owner) : IDisposable
{
    private int state = (int)OrdinaryBinaryLeaseState.Reserved;
    internal OrdinaryBinaryBacking Owner => owner;

    // The owner's actual native refcount has already been incremented while
    // holding its gate. A racing Dispose cannot release until that gate exits.
    internal bool TryAdmit() => Interlocked.CompareExchange(ref state, (int)OrdinaryBinaryLeaseState.Admitted,
        (int)OrdinaryBinaryLeaseState.Reserved) == (int)OrdinaryBinaryLeaseState.Reserved;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref state, (int)OrdinaryBinaryLeaseState.Disposed) == (int)OrdinaryBinaryLeaseState.Admitted) owner.Release();
    }
}
