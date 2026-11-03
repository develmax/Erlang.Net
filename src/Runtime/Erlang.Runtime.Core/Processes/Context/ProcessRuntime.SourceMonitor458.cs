namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<Term> MonitorSourceAsync458(
        ProcessContext owner,
        string kind,
        Term target,
        ProcessMonitorOptions options
    )
    {
        var input641 = PrepareMonitorInput641(owner, target, options);
        try
        {
            target = ((TupleTerm)input641.Value641).Items[0];
            options = PreparedMonitorOptions641(options, input641);
            if (kind == ProcessMonitorKinds.Process)
            {
                var connected = await DistributionPending.TrySourceMonitor458(owner, target, options).ConfigureAwait(false);
                if (connected is not null) return connected;
            }

            return MonitorSource(
                owner,
                kind,
                target,
                options
            );
        }
        finally
        {
            input641.Close641();
        }
    }

    private void AddConnectedOriginMonitor458(DistributionProcessMonitor monitor, ProcessMonitorOptions? options)
    {
        distributionProcessMonitors.Add(monitor);
        if (options is null) return;
        monitor.SpawnOptions = true;
        Term reported = monitor.Watched is Atom name ? Term.Tuple(name, monitor.Entry.Node) : monitor.Watched;
        AddMonitor641(
            monitor.Reference,
            CreateMonitorEntry641(
                monitor.Local,
                monitor.Watched as Pid,
                reported,
                options
            )
        );
    }

    internal async ValueTask<ReferenceTerm> BeginSourceMonitor458(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        ProcessContext observer,
        Term remote,
        ProcessMonitorOptions options
    )
    {
        var monitor = CreateConnectedOriginMonitor(
            entry,
            connected,
            observer,
            remote,
            out bool publish,
            options
        );
        if (!publish) return monitor.Reference;
        try
        {
            bool committed = await entry.Owner.PublishLiveControl(
                entry,
                connected,
                monitor.ConnectionId,
                observer,
                Term.Tuple(
                    Term.I(DistributionControlOpcodes.Monitor),
                    observer.Self,
                    remote,
                    monitor.Reference
                ),
                observer.Self,
                stateValid: () => !distributionProcessMonitors.Contains(monitor),
                commit: () => AddConnectedOriginMonitor458(monitor, options)
            ).ConfigureAwait(false);
            if (!committed)
            {
                lock (gate)
                {
                    if (!IsOwnedLiveNodeProcess(observer))
                    {
                        aliases.Remove(monitor.Reference);
                        throw new OperationCanceledException(observer.Cancellation);
                    }
                    AddConnectedOriginMonitor458(monitor, options);
                    QueueConnectedMonitorDown(monitor, remote, Term.A(ProcessMonitorFailureReasons.Disconnected));
                }
            }

            return monitor.Reference;
        }
        catch
        {
            lock (gate)
                if (!monitors.ContainsKey(monitor.Reference)) aliases.Remove(monitor.Reference);
            throw;
        }
    }
}
