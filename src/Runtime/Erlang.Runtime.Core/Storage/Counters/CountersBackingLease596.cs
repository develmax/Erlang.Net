namespace Erlang;

// Actual allocation custody, distinct from short active-operation pins421.
internal sealed class CountersBackingLease596(CountersArray421 array, CountersLeaseKind596 kind) : IDisposable
{
    private CountersArray421? current = array;
    internal CountersArray421 Value => Volatile.Read(ref current) ?? throw new ObjectDisposedException(nameof(CountersBackingLease596));
    internal CountersLeaseKind596 Kind { get; } = kind;

    internal bool IsFor596(CountersArray421 candidate) => ReferenceEquals(Volatile.Read(ref current), candidate);

    public void Dispose() => Interlocked.Exchange(ref current, null)?.ReleaseBacking596(Kind);
}
