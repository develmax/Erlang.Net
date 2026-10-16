namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly List<DistributionProcessLink> distributionProcessLinks = new();

    internal void QueueConnectedProcessLink(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        Pid remote,
        Pid local
    )
    {
        lock (gate)
        {
            if (!IsCurrentLocal(local) || !processes.TryGetValue(local, out var target))
            {
                QueueDistributionLinkReply(
                    entry,
                    connected,
                    entry.ConnectionId,
                    local,
                    remote,
                    Term.A(ErlangErrorReasons.NoProcess)
                );

                return;
            }
            if (distributionProcessLinks.Any(link => ReferenceEquals(link.Entry, entry) &&
                ReferenceEquals(link.Connection, connected) && ReferenceEquals(link.Local, target) &&
                link.Remote.Equals(remote) && link.UnlinkId is null))
                return;
            var link = new DistributionProcessLink(
                entry,
                connected,
                entry.ConnectionId,
                target,
                remote
            );
            distributionProcessLinks.Add(link);
            target.PendingLinkOrigins.Add(link.Identity, remote);
            target.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                lock (gate)
                {
                    target.PendingLinkOrigins.Remove(link.Identity);
                    if (!IsOwnedLiveNodeProcess(target) || link.UnlinkId is not null || link.Identity.PendingUnlink is not null)
                        return null;
                    if (target.LinkIdentities.TryGetValue(remote, out var current) && !ReferenceEquals(current, link.Identity) &&
                        (current.PendingUnlink is null || !distributionProcessLinks.Contains(link)))
                        return null;
                    // An accepted LINK precedes its queued retirement EXIT even after removal from the live connection list.
                    target.LinkIdentities[remote] = link.Identity;
                    target.Links.Add(remote);
                    link.Established = true;

                    return null;
                }
            }
            );
        }
    }

    internal void QueueConnectedProcessUnlink(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        Pid remote,
        Pid local,
        ulong unlinkId,
        bool acknowledgement
    )
    {
        lock (gate)
        {
            uint connectionId = entry.ConnectionId;
            if (!IsCurrentLocal(local) || !processes.TryGetValue(local, out var target))
            {
                if (!acknowledgement && IsCurrentLocal(local))
                    QueueDistributionUnlinkReply(
                        entry,
                        connected,
                        connectionId,
                        local,
                        remote,
                        unlinkId
                    );

                return;
            }
            var link = distributionProcessLinks.LastOrDefault(item =>
                ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                item.ConnectionId == connectionId && ReferenceEquals(item.Local, target) &&
                item.Remote.Equals(remote) && (!acknowledgement || item.UnlinkId == unlinkId));
            target.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                lock (gate)
                {
                    if (link is not null && (acknowledgement || link.Identity.PendingUnlink is null))
                    {
                        distributionProcessLinks.Remove(link);
                        RemoveDistributionLinkIdentity(link);
                    }
                    if (!acknowledgement)
                        QueueDistributionUnlinkReply(
                            entry,
                            connected,
                            connectionId,
                            local,
                            remote,
                            unlinkId
                        );

                    return null;
                }
            }
            );
        }
    }

    private ulong nextDistributionUnlinkId;



    internal void QueueConnectedProcessExit(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        Pid remote,
        Pid local,
        Term reason,
        bool explicitSignal,
        Term? traceToken = null
    )
    {
        lock (gate)
        {
            if (!IsCurrentLocal(local) || !processes.TryGetValue(local, out var target))
                return;
            var link = explicitSignal ? null : distributionProcessLinks.LastOrDefault(item =>
                ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                item.ConnectionId == entry.ConnectionId && ReferenceEquals(item.Local, target) &&
                item.Remote.Equals(remote));
            if (!explicitSignal && link is null)
                return;
            QueueDistributionExitSignal(
                target,
                remote,
                reason,
                explicitSignal,
                link,
                traceToken
            );
        }
    }

    private void QueueDistributionExitSignal(
        ProcessContext target,
        Pid remote,
        Term reason,
        bool explicitSignal,
        DistributionProcessLink? link,
        Term? traceToken = null
    )
    {
        target.Mailbox.SendPending(
            Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), remote, reason),
            () =>
        {
            lock (gate)
            {
                if (!IsOwnedLiveNodeProcess(target))
                    return null;
                bool priority = false;
                if (link is not null)
                {
                    distributionProcessLinks.Remove(link);
                    if (!target.LinkIdentities.TryGetValue(remote, out var current) ||
                        !ReferenceEquals(current, link.Identity))
                        return null;
                    if (current.PendingUnlink is not null || !target.Links.Contains(remote))
                    {
                        RemoveDistributionLinkIdentity(link);

                        return null;
                    }
                    priority = target.PriorityLinks.Contains(remote);
                    RemoveDistributionLinkIdentity(link);
                }
                bool kill = explicitSignal && reason.Equals(Term.A(ProcessExitSignals.Kill));
                if (target.TrapExits && !kill)
                    return priority;
                if (!kill && reason.Equals(Term.A(ProcessExitReasons.Normal)))
                    return null;
                Terminate(target, kill ? Term.A(ProcessExitReasons.Killed) : reason);

                return null;
            }
        },
            from: remote,
            distributionTraceToken: traceToken
        );
    }

    private static void RemoveDistributionLinkIdentity(DistributionProcessLink link)
    {
        var target = link.Local;
        if (target.LinkIdentities.TryGetValue(link.Remote, out var current) && ReferenceEquals(current, link.Identity))
        {
            target.LinkIdentities.Remove(link.Remote);
            target.Links.Remove(link.Remote);
            target.PriorityLinks.Remove(link.Remote);
        }
        target.PendingLinkOrigins.Remove(link.Identity);
    }

    internal void RetireConnectedProcessLinks(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected
    )
    {
        lock (gate)
        {
            foreach (var link in distributionProcessLinks.Where(item => ReferenceEquals(item.Entry, entry) &&
                ReferenceEquals(item.Connection, connected)).ToArray())
            {
                distributionProcessLinks.Remove(link);
                QueueDistributionExitSignal(
                    link.Local,
                    link.Remote,
                    Term.A(ProcessLinkFailureReasons.Disconnected),
                    false,
                    link
                );
            }
        }
    }



    private void QueueDistributionLinkReply(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        Pid local,
        Pid remote,
        Term reason
    ) => entry.Owner.ScheduleConnectedControlSignal(
        entry,
        connected,
        connectionId,
        Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit),
            local,
            remote,
            reason
        ),
        local
    );

    private void QueueDistributionUnlinkReply(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        uint connectionId,
        Pid local,
        Pid remote,
        ulong unlinkId
    ) => entry.Owner.ScheduleConnectedControlSignal(
        entry,
        connected,
        connectionId,
        Term.Tuple(
            Term.I(DistributionControlOpcodes.UnlinkIdAck),
            new Integer(unlinkId),
            local,
            remote
        ),
        local
    );
}
