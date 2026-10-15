namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ReferenceTerm BeginConnectedProcessMonitor(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected, ProcessContext observer, Term remote)
    {
        DistributionProcessMonitor monitor;
        lock (gate)
            monitor = new(entry, connected, entry.ConnectionId, observer, remote, remote, MakeReference(), true);
        bool committed = entry.Owner.TryPublishImmediateControl(entry, connected, monitor.ConnectionId, observer,
            Term.Tuple(Term.I(DistributionControlOpcodes.Monitor), observer.Self, remote, monitor.Reference),
            () => !distributionProcessMonitors.Contains(monitor), () => distributionProcessMonitors.Add(monitor));
        if (!committed)
            throw new InvalidOperationException(DistributionConnectedDiagnostics.Connected);

        return monitor.Reference;
    }

    internal async ValueTask<ReferenceTerm> BeginConnectedProcessMonitorAsync(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected, ProcessContext observer, Term remote)
    {
        DistributionProcessMonitor monitor;
        lock (gate)
            monitor = new(entry, connected, entry.ConnectionId, observer, remote, remote, MakeReference(), true);
        bool committed = await entry.Owner.PublishLiveControl(entry, connected, monitor.ConnectionId, observer,
            Term.Tuple(Term.I(DistributionControlOpcodes.Monitor), observer.Self, remote, monitor.Reference), observer.Self,
            stateValid: () => !distributionProcessMonitors.Contains(monitor),
            commit: () => distributionProcessMonitors.Add(monitor)).ConfigureAwait(false);
        if (!committed)
            throw new InvalidOperationException(DistributionConnectedDiagnostics.Connected);

        return monitor.Reference;
    }

    private DistributionProcessMonitor? FindConnectedOriginMonitor(ProcessContext observer, ReferenceTerm reference)
    {
        lock (gate)
            return distributionProcessMonitors.FirstOrDefault(item => item.Origin && ReferenceEquals(item.Local, observer) && item.Reference.Equals(reference));
    }

    internal bool EndConnectedProcessMonitor(ProcessContext observer, ReferenceTerm reference)
    {
        var monitor = FindConnectedOriginMonitor(observer, reference);
        if (monitor is null)
            return false;

        return monitor.Entry.Owner.TryPublishImmediateControl(monitor.Entry, monitor.Connection, monitor.ConnectionId, observer,
            Term.Tuple(Term.I(DistributionControlOpcodes.Demonitor), observer.Self, monitor.Remote, reference),
            () => distributionProcessMonitors.Contains(monitor), () => distributionProcessMonitors.Remove(monitor));
    }

    internal async ValueTask<bool> EndConnectedProcessMonitorAsync(ProcessContext observer, ReferenceTerm reference)
    {
        var monitor = FindConnectedOriginMonitor(observer, reference);
        if (monitor is null)
            return false;

        return await monitor.Entry.Owner.PublishLiveControl(monitor.Entry, monitor.Connection, monitor.ConnectionId, observer,
            Term.Tuple(Term.I(DistributionControlOpcodes.Demonitor), observer.Self, monitor.Remote, reference), observer.Self,
            stateValid: () => distributionProcessMonitors.Contains(monitor),
            commit: () => distributionProcessMonitors.Remove(monitor)).ConfigureAwait(false);
    }

    private DistributionProcessLink? FindConnectedUnlink(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected, ProcessContext local, Pid remote)
    {
        lock (gate)
            return distributionProcessLinks.LastOrDefault(item => ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                ReferenceEquals(item.Local, local) && item.Remote.Equals(remote) && item.Established && item.UnlinkId is null);
    }

    private ulong ReserveDistributionUnlinkId()
    {
        lock (gate)
        {
            nextDistributionUnlinkId = unchecked(nextDistributionUnlinkId + 1);
            if (nextDistributionUnlinkId == 0)
                nextDistributionUnlinkId++;

            return nextDistributionUnlinkId;
        }
    }

    private bool DistributionUnlinkStillActive(DistributionProcessLink link)
        => distributionProcessLinks.Contains(link) && link.UnlinkId is null && link.Local.Links.Contains(link.Remote) &&
            link.Local.LinkIdentities.TryGetValue(link.Remote, out var identity) && ReferenceEquals(identity, link.Identity);

    private static void CommitDistributionUnlink(DistributionProcessLink link, ulong unlinkId)
    {
        link.UnlinkId = unlinkId;
        link.Identity.PendingUnlink = new ProcessUnlinkIdentity();
        link.Local.Links.Remove(link.Remote);
        link.Local.PriorityLinks.Remove(link.Remote);
    }

    internal ulong? BeginConnectedProcessUnlink(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected, ProcessContext local, Pid remote)
    {
        var link = FindConnectedUnlink(entry, connected, local, remote);
        if (link is null)
            return null;
        ulong unlinkId = ReserveDistributionUnlinkId();
        bool committed = entry.Owner.TryPublishImmediateControl(entry, connected, link.ConnectionId, local,
            Term.Tuple(Term.I(DistributionControlOpcodes.UnlinkId), new Integer(unlinkId), local.Self, remote),
            () => DistributionUnlinkStillActive(link), () => CommitDistributionUnlink(link, unlinkId));

        return committed ? unlinkId : null;
    }

    internal async ValueTask<ulong?> BeginConnectedProcessUnlinkAsync(RuntimeDistributionPendingEntry entry, DistributionConnectedEntry connected, ProcessContext local, Pid remote)
    {
        var link = FindConnectedUnlink(entry, connected, local, remote);
        if (link is null)
            return null;
        ulong unlinkId = ReserveDistributionUnlinkId();
        bool committed = await entry.Owner.PublishLiveControl(entry, connected, link.ConnectionId, local,
            Term.Tuple(Term.I(DistributionControlOpcodes.UnlinkId), new Integer(unlinkId), local.Self, remote), local.Self,
            stateValid: () => DistributionUnlinkStillActive(link), commit: () => CommitDistributionUnlink(link, unlinkId)).ConfigureAwait(false);

        return committed ? unlinkId : null;
    }
}
