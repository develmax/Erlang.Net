namespace Erlang;

// A real resource ownership header, not an estimated ERTS word allocation.
// Its lease survives lexical root retirement and is released by the guest GC.
internal sealed class ProcessNativeHeader524(long identity, Term term, IDisposable lease) : IDisposable
{
    private IDisposable? root = lease;
    internal long Identity524 { get; } = identity;
    internal Term Term524 { get; } = term;
    internal bool Old524 { get; set; }

    internal void DisposeGuestGc536()
    {
        IDisposable? selected = Interlocked.Exchange(ref root, null);
        if (selected is NativeResourceRootLease524 actual) actual.DisposeGuestGc536();
        else selected?.Dispose();
    }

    public void Dispose() => Interlocked.Exchange(ref root, null)?.Dispose();
}
