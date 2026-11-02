namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Mirrors the real frame/heap admission required by the slow temporary
    // borrow/import route. Closing or unregistered frames take that old route.
    internal bool IsRegisteredOpenImmediateFrame157(ProcessTermFrame506 frame)
    {
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(CellStorage526Names.GateRequired);

        return !closing && !frame.IsDisposed && frames.Contains(frame);
    }
}
