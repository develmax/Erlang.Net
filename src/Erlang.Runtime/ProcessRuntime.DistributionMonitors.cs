namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly List<DistributionProcessMonitor> distributionProcessMonitors = new();

    internal void QueueConnectedProcessMonitor(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        Pid watcher,
        Term watched,
        ReferenceTerm reference,
        bool remove
    )
    {
        lock (gate)
        {
            Pid? local = watched is Atom name ? WhereIs(name.Name) as Pid : watched as Pid;
            if (remove)
            {
                var prior = distributionProcessMonitors.FirstOrDefault(item => !item.Origin &&
                    ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                    item.Reference.Equals(reference) &&
                    (watched is Atom || local is not null && item.Local.Self.Equals(local)));
                if (prior is not null)
                    prior.Local.Mailbox.SendPending(Nil.Value, () =>
                    {
                        lock (gate)
                            distributionProcessMonitors.Remove(prior);

                        return null;
                    });

                return;
            }
            if (local is null || !IsCurrentLocal(local) || !processes.TryGetValue(local, out var target))
            {
                SendConnectedMonitorControl(
                    entry,
                    connected,
                    entry.ConnectionId,
                    DistributionControlOpcodes.MonitorExit,
                    watched,
                    watcher,
                    reference,
                    Term.A(ErlangErrorReasons.NoProcess)
                );

                return;
            }
            if (distributionProcessMonitors.Any(item => !item.Origin &&
                ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                item.Reference.Equals(reference)))
                throw new NotSupportedException(DistributionMonitorDiagnostics.DuplicateReference);
            var monitor = new DistributionProcessMonitor(
                entry,
                connected,
                entry.ConnectionId,
                target,
                watcher,
                watched,
                reference,
                false
            );
            distributionProcessMonitors.Add(monitor);
            target.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                lock (gate)
                {
                    if (distributionProcessMonitors.Contains(monitor) && IsOwnedLiveNodeProcess(target))
                        monitor.Established = true;
                }

                return null;
            }
            );
        }
    }





    internal void QueueConnectedProcessMonitorDown(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        Term watched,
        Pid watcher,
        ReferenceTerm reference,
        Term reason
    )
    {
        lock (gate)
        {
            if (!IsCurrentLocal(watcher) || !processes.TryGetValue(watcher, out var observer))
                return;
            var monitor = distributionProcessMonitors.FirstOrDefault(item => item.Origin &&
                ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                ReferenceEquals(item.Local, observer) && item.Reference.Equals(reference));
            if (monitor is not null)
                QueueConnectedMonitorDown(monitor, watched, reason);
        }
    }

    private void QueueConnectedMonitorDown(DistributionProcessMonitor monitor, Term watched, Term reason)
    {
        Term reported = watched is Atom name ? Term.Tuple(name, Term.A(monitor.Entry.Node.Name)) : watched;
        monitor.Local.Mailbox.SendPending(
            Term.Tuple(
                Term.A(ProcessMessageTags.MonitorDown),
                monitor.Reference,
                Term.A(ProcessMonitorKinds.Process),
                reported,
                reason
            ),
            () =>
            {
                lock (gate)
                {
                    if (!distributionProcessMonitors.Remove(monitor) || !IsOwnedLiveNodeProcess(monitor.Local))
                        return null;

                    return false;
                }
            },
            preserve: true,
            from: monitor.Entry.Node
        );
    }

    internal void RetireConnectedProcessMonitors(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected)
    {
        lock (gate)
        {
            foreach (var monitor in distributionProcessMonitors.Where(item =>
                ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected)).ToArray())
            {
                if (monitor.Origin)
                    QueueConnectedMonitorDown(monitor, monitor.Watched, Term.A(ProcessMonitorFailureReasons.Disconnected));
                else
                    distributionProcessMonitors.Remove(monitor);
            }
        }
    }



    private void SendConnectedMonitorControl(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        int opcode,
        Term from,
        Term target,
        ReferenceTerm reference,
        Term? reason = null
    )
    {
        if ((connected.Flags & DistributionPassThroughFlags.ProcessMonitors) != DistributionPassThroughFlags.ProcessMonitors)
            return;
        TupleTerm control = reason is null
            ? Term.Tuple(
                Term.I(opcode),
                from,
                target,
                reference
            )
            : Term.Tuple(
                Term.I(opcode),
                from,
                target,
                reference,
                reason
            );
        entry.Owner.ScheduleConnectedControlSignal(
            entry,
            connected,
            connectionId,
            control,
            from is Pid sender && IsCurrentLocal(sender) ? sender : connected.Controller.Self
        );
    }
}
