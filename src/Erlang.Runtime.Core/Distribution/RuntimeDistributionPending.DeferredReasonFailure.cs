namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal Action<Exception> CaptureDeferredReasonFailure(Term handle)
    {
        lock (gate)
        {
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var captured = RequireConnected(entry, connectionId);

                return _ => RejectDeferredReasonIfCurrent(entry, captured, connectionId);
            }
        }
    }

    private void RejectDeferredReasonIfCurrent(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry captured,
        uint connectionId
    )
    {
        lock (gate)
        {
            if (entry.ConnectionId != connectionId || !ReferenceEquals(entry.Connected, captured) ||
                entry.State != DistributionPendingState.Connected)
                return;
            entry.State = DistributionPendingState.Exiting;
            if (captured.Controller is { } controller)
            {
                exitingInputControllers.Add(controller);
                runtime.QueueDistributionControllerKill(controller);
            }
            else RetirePending(entry);
        }
    }
}
