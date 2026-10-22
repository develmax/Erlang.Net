namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal void QueueCloseSignal(Term identity, Term reason, DriverParentPeerTransport422 transport)
    {
        lock (gate)
        {
            var row = Require(identity);
            row.Reason = reason;
            // CloseOrdered registers the actual operation before returning.
            // Global/actor retirement joins it even when no caller awaits it.
            _ = CloseOrdered(identity, transport, CancellationToken.None);
        }
    }
}
