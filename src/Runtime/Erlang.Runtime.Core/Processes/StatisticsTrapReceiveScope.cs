namespace Erlang;

internal sealed class StatisticsTrapReceiveScope(
    AsyncLocal<StatisticsTrapReceiveFailure?> slot,
    StatisticsTrapReceiveFailure? previous) : IDisposable
{
    private bool closed;

    public void Dispose()
    {
        if (closed)
            return;
        closed = true;
        slot.Value = previous;
    }
}
