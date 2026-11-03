namespace Erlang;

// Actual allocation custody, distinct from short active-operation pins413.
internal sealed class AtomicsBackingLease586(AtomicsArray413 array, AtomicsLeaseKind586 kind) : IDisposable
{
    private AtomicsArray413? current = array;
    internal AtomicsArray413 Value => Volatile.Read(ref current) ?? throw new ObjectDisposedException(nameof(AtomicsBackingLease586));
    internal AtomicsLeaseKind586 Kind { get; } = kind;

    internal bool IsFor586(AtomicsArray413 candidate) => ReferenceEquals(Volatile.Read(ref current), candidate);

    public void Dispose() => Interlocked.Exchange(ref current, null)?.ReleaseBacking586(Kind);
}
