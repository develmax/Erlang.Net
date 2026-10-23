namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void NotifyConnectedNode(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        string eventName,
        Term? reason = null
    )
    {
        var visibility = (connected.Flags & DistributionConnectionFlags.Published) != 0
            ? RuntimeNodeMonitorType.Visible
            : RuntimeNodeMonitorType.Hidden;
        runtime.NotifyConnectedNode(
            eventName,
            entry.Node,
            visibility,
            entry.ConnectionId,
            reason
        );
    }

    private static Term ControllerNodeDownReason(Term reason)
    {
        if (reason is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: ProcessExitReasons.Shutdown })
            reason = tuple.Items[1];

        return reason is Atom { Name: ProcessExitReasons.Normal } ? Term.A(DistributionConnectionReasons.Closed) : reason;
    }

    private static Term KernelNodeDownReason(Term reason)
        => reason is Atom { Name: RuntimeNodeMonitorNames.NoNetwork } ? reason : Term.A(RuntimeNodeMonitorNames.KernelTerminated);
}
