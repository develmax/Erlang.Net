namespace Erlang;

internal sealed class BinaryProgramLease598(BinaryProgramBacking598 owner, BinaryProgramLeaseKind598 kind) : IDisposable
{
    private BinaryProgramBacking598? backing = owner;

    internal bool Owns598(BinaryProgramBacking598 expected) => ReferenceEquals(Volatile.Read(ref backing), expected);

    public void Dispose() => Interlocked.Exchange(ref backing, null)?.Release598(kind);
}
