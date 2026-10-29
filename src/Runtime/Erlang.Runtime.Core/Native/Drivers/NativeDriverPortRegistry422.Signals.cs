namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal void QueueCloseSignal(
        Term identity,
        Term reason,
        DriverParentPeerTransport422 transport,
        Pid? sender = null,
        RuntimeProcessPayload641? reason641 = null
    )
    {
        RuntimeProcessPayload641? queued641 = reason641?.Borrow641(null);
        try
        {
            lock (gate)
            {
                var row = Require(identity);
                if (sender is null)
                {
                    row.StopRequested = true;
                    row.Writable.TrySetResult();
                }
                // Keep a signal-owned borrow through the actual queue task.
                Task<Term> queued = EnqueueSignal454(
                    row,
                    sender,
                    false,
                    async () =>
                    {
                        lock (gate)
                        {
                            if (row.Closing || row.PhysicallyStopped)
                                return Term.A(ErlangBooleanAtoms.True);

                            row.SetReason641(reason, queued641);
                        }
                        await Close(row, transport).ConfigureAwait(false);

                        return Term.A(ErlangBooleanAtoms.True);
                    }
                );
                queued641?.CloseAfter641(queued);
            }
        }
        catch
        {
            queued641?.Close641();

            throw;
        }
    }
}
