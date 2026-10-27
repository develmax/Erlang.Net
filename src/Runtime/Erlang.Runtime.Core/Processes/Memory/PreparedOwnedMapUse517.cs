namespace Erlang;

internal sealed class PreparedOwnedMapUse517(PreparedOwnedMap517 owner) : IDisposable
{
    private PreparedOwnedMap517? current = owner;

    internal bool IsFor517(PreparedOwnedMap517 owner) => ReferenceEquals(Volatile.Read(ref current), owner);

    public void Dispose() => Interlocked.Exchange(ref current, null)?.LeaveUse517();
}
