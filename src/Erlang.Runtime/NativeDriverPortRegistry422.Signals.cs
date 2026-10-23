namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal void QueueCloseSignal(
        Term identity,
        Term reason,
        DriverParentPeerTransport422 transport,
        Pid? sender = null
    )
    {
        lock (gate)
        {
            var row = Require(identity);
            if (sender is null)
            {
                row.StopRequested = true;
                row.Writable.TrySetResult();
            }
            // Guest exit retains its actual sender dependency. Host teardown
            // stops busy work immediately and still joins the physical fence.
            _ = EnqueueSignal454(
                row,
                sender,
                false,
                async () =>
            {
                lock (gate)
                {
                    if (row.Closing || row.PhysicallyStopped) return Term.A(ErlangBooleanAtoms.True);
                    row.Reason = reason;
                }
                await Close(row, transport).ConfigureAwait(false);

                return Term.A(ErlangBooleanAtoms.True);
            }
            );
        }
    }
}
