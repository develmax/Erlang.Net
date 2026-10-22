namespace Erlang;

internal sealed class CountersInvocation421(RuntimeCountersOwner421 owner) : IDisposable
{
    private RuntimeCountersOwner421? current = owner;

    public void Dispose() => Interlocked.Exchange(ref current, null)?.Leave();
}
