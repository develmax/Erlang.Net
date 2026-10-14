namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void NotifyConnectedNode(
        string eventName,
        Atom node,
        RuntimeNodeMonitorType visibility,
        uint connectionId,
        Term? reason
    ) => localNodeEvents?.NotifyRemote(
        eventName,
        node,
        visibility,
        connectionId,
        reason
    );

    internal ProcessContext RequireDistributionKernel(bool requireRegistered = false)
    {
        lock (gate)
        {
            if (localNodeKernel is not { } kernel || kernel.DistributionRole != RuntimeDistributionRole.Kernel ||
                !IsOwnedLiveNodeProcess(kernel) || requireRegistered && !WhereIs(RuntimeNodeControlNames.Kernel).Equals(kernel.Self))
                throw new InvalidOperationException(DistributionConnectedDiagnostics.Kernel);

            return kernel;
        }
    }
}
