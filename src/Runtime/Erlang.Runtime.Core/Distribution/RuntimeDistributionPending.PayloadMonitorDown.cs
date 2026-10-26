namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void RouteDeferredConnectedMonitorDown(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        DistributionPayloadExitEnvelope envelope
    )
    {
        var items = envelope.Control.Items;
        Term watched = items[DistributionControlOpcodes.FromIndex];
        var watcher = (Pid)items[DistributionControlOpcodes.TargetIndex];
        var reference = (ReferenceTerm)items[DistributionControlOpcodes.MonitorReferenceIndex];
        if (watched is Pid pid && runtime.IsCurrentLocal(pid) || watcher.Node != runtime.Node ||
            watcher.LocalIdentity is not null && !ReferenceEquals(watcher.LocalIdentity, runtime.LocalIdentity))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        runtime.QueueConnectedDeferredMonitorDown(
            entry,
            connected,
            watched,
            watcher,
            reference,
            envelope.Reason,
            CaptureDeferredReasonFailure(Term.Tuple(Term.I(entry.ConnectionId), entry.Reference))
        );
    }
}
