namespace Erlang;

// Only an EXPORTED HOST WRAPPER retains this carrier. Never put it in the
// canonical registry Entry or guest backing object, where stale CLR wrappers
// would accidentally keep a guest allocation alive after real guest GC.
internal sealed class CountersHostCustody596 : IDisposable
{
    private CountersBackingLease596? retained;

    internal CountersHostCustody596(CountersGuestRoot421 root) => retained = root.Array.RetainBacking596(CountersLeaseKind596.Host);

    internal bool Owns596(CountersGuestRoot421 root) => Volatile.Read(ref retained)?.IsFor596(root.Array) == true;

    ~CountersHostCustody596()
    {
        try
        {
            Dispose();
        }
        catch { /* owner physical retirement records/joins failures */ }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref retained, null)?.Dispose();
        GC.SuppressFinalize(this);
    }
}
