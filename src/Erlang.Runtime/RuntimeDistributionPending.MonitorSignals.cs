namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    public ReferenceTerm BeginConnectedMonitorSignal(Term handle, ProcessContext observer, Term remote)
    {
        using var target = CaptureMonitorPublicationTarget(handle, observer, remote);

        return runtime.BeginConnectedProcessMonitor(
            target.Entry,
            target.Connection,
            observer,
            remote
        );
    }

    public bool EndConnectedMonitorSignal(ProcessContext observer, ReferenceTerm reference)
    {
        ValidateMonitorRemoval(observer, reference);

        return runtime.EndConnectedProcessMonitor(observer, reference);
    }

    private void RouteRawConnectedMonitorControl(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        TupleTerm control
    )
    {
        var items = control.Items;
        int opcode = (int)((Integer)items[DistributionControlOpcodes.OpcodeIndex]).Value;
        Term from = items[DistributionControlOpcodes.FromIndex];
        Term target = items[DistributionControlOpcodes.TargetIndex];
        var reference = (ReferenceTerm)items[DistributionControlOpcodes.MonitorReferenceIndex];

        bool Local(Pid pid) => pid.Node == runtime.Node &&
            (pid.LocalIdentity is null || ReferenceEquals(pid.LocalIdentity, runtime.LocalIdentity));
        if (opcode == DistributionControlOpcodes.MonitorExit)
        {
            if (from is Pid watched && watched.LocalIdentity is not null || target is not Pid downWatcher || !Local(downWatcher))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            runtime.QueueConnectedProcessMonitorDown(
                entry,
                connected,
                from,
                downWatcher,
                reference,
                items[DistributionControlOpcodes.MonitorReasonIndex]
            );
        }
        else
        {
            if (from is not Pid requestWatcher || requestWatcher.LocalIdentity is not null || requestWatcher.Node != entry.Node.Name ||
                runtime.IsCurrentLocal(requestWatcher) || target is Pid local && !Local(local))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            runtime.QueueConnectedProcessMonitor(
                entry,
                connected,
                requestWatcher,
                target,
                reference,
                opcode == DistributionControlOpcodes.Demonitor
            );
        }
    }
}
