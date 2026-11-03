namespace Erlang;

// Retain the source host wrapper/carrier until AFTER the short active operation
// lease is disposed. Entry-only421 leases otherwise lose that CLR lifetime edge.
internal sealed class CountersResourceLease596(ReferenceResourceLease<CountersGuestRoot421> lease, ReferenceTerm source) : IDisposable
{
    private ReferenceResourceLease<CountersGuestRoot421>? current = lease;
    private ReferenceTerm? sourceRoot = source;
    internal CountersGuestRoot421 Value => (Volatile.Read(ref current) ?? throw new ObjectDisposedException(nameof(CountersResourceLease596))).Value;

    public void Dispose()
    {
        Interlocked.Exchange(ref current, null)?.Dispose();
        GC.KeepAlive(sourceRoot);
        sourceRoot = null;
    }
}
