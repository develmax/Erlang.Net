namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    public ReferenceTerm BeginConnectedMonitorSignal(Term handle, ProcessContext observer, Term remote)
    {
        lock (gate)
        {
            RequireProcess(observer);
            var entry = ReadHandle(handle, out uint connectionId, out var resource);
            using (resource)
            {
                var connected = RequireConnected(entry, connectionId);
                if (remote is not Atom && (remote is not Pid pid || pid.LocalIdentity is not null ||
                    pid.Node != entry.Node.Name || runtime.IsCurrentLocal(pid)))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return runtime.BeginConnectedProcessMonitor(
                    entry,
                    connected,
                    observer,
                    remote
                );
            }
        }
    }

    public bool EndConnectedMonitorSignal(ProcessContext observer, ReferenceTerm reference)
    {
        lock (gate)
        {
            RequireProcess(observer);
            if (!runtime.IsCurrentLocal(reference))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return runtime.EndConnectedProcessMonitor(observer, reference);
        }
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
