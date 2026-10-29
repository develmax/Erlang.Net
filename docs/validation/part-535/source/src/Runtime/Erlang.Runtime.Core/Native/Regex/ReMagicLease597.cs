namespace Erlang;

internal sealed class ReMagicLease597(ReMagicBacking597 value, ReMagicLeaseKind597 kind) : IDisposable
{
    private ReMagicBacking597? backing = value;

    internal bool Owns(ReMagicBacking597 expected) => ReferenceEquals(Volatile.Read(ref backing), expected);

    public void Dispose() => Interlocked.Exchange(ref backing, null)?.Release(kind);
}
