namespace Erlang;

internal sealed class DebugBreakpointExecution432 : IDisposable
{
    private RuntimeDebugStop432? first;

    internal void Track(RuntimeDebugStop432 stop)
    {
        stop.ScopeNext = first;
        first = stop;
    }

    public void Dispose()
    {
        while (first is { } stop)
        {
            first = stop.ScopeNext;
            stop.ScopeNext = null;
            stop.Owner.CompleteStop(stop);
        }
    }
}
