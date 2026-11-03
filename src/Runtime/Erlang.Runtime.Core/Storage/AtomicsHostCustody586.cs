namespace Erlang;

// Only an EXPORTED HOST WRAPPER retains this carrier. Never put it in the
// canonical registry Entry or guest backing object, where stale CLR wrappers
// would accidentally keep a guest allocation alive after real guest GC.
internal sealed class AtomicsHostCustody586 : IDisposable
{
    private AtomicsBackingLease586? retained;

    internal AtomicsHostCustody586(AtomicsGuestRoot413 root) => retained = root.Array.RetainBacking586(AtomicsLeaseKind586.Host);

    internal bool Owns586(AtomicsGuestRoot413 root) => Volatile.Read(ref retained)?.IsFor586(root.Array) == true;

    ~AtomicsHostCustody586()
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
