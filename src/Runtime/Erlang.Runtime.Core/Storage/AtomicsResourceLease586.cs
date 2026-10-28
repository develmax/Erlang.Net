namespace Erlang;

// Retain the source host wrapper/carrier until AFTER the short active operation
// lease is disposed. Entry-only413 leases otherwise lose that CLR lifetime edge.
internal sealed class AtomicsResourceLease586(ReferenceResourceLease<AtomicsGuestRoot413> lease, ReferenceTerm source) : IDisposable
{
    private ReferenceResourceLease<AtomicsGuestRoot413>? current = lease;
    private ReferenceTerm? sourceRoot = source;
    internal AtomicsGuestRoot413 Value => (Volatile.Read(ref current) ?? throw new ObjectDisposedException(nameof(AtomicsResourceLease586))).Value;

    public void Dispose()
    {
        Interlocked.Exchange(ref current, null)?.Dispose();
        GC.KeepAlive(sourceRoot);
        sourceRoot = null;
    }
}
