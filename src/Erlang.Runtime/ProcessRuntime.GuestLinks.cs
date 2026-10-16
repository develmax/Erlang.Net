namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask LinkGuestAsync(ProcessContext caller, Pid remote, bool priority = false)
    {
        ValueTask<bool> publication;
        DistributionProcessLink link;
        lock (gate)
        {
            ValidateGuestLinkCaller(caller, remote);
            if (remote.Node == Node || !DistributionPending.TryCaptureGuestLinkConnection(remote, out var entry, out var connected))
            {
                Link(caller, remote, priority);

                return;
            }
            var existing = FindGuestDistributionLink(
                entry,
                connected,
                caller,
                remote
            );
            if (existing is not null && existing.UnlinkId is null && existing.Identity.PendingUnlink is null)
            {
                // An inbound LINK already establishes the peer's half, even before its mailbox callback runs.
                InstallGuestDistributionLink(existing, priority);

                return;
            }
            link = new DistributionProcessLink(
                entry,
                connected,
                entry.ConnectionId,
                caller,
                remote
            );
            distributionProcessLinks.Add(link);
            InstallGuestDistributionLink(link, priority);
            try
            {
                // Relation and receiver priority exist before reserve/wait, as in the pinned external LINK branch.
                publication = entry.Owner.PublishLiveControl(
                    entry,
                    connected,
                    link.ConnectionId,
                    caller,
                    Term.Tuple(Term.I(DistributionControlOpcodes.Link), caller.Self, remote),
                    caller.Self,
                    stateValid: () => DistributionUnlinkStillActive(link)
                );
            }
            catch
            {
                RollBackUnpublishedGuestLink(link);
                throw;
            }
        }
        try
        {
            _ = await publication.ConfigureAwait(false);
        }
        catch
        {
            lock (gate)
                RollBackUnpublishedGuestLink(link);
            throw;
        }
    }

    internal async ValueTask UnlinkGuestAsync(ProcessContext caller, Pid remote)
    {
        ValueTask<Task> preparation;
        lock (gate)
        {
            ValidateGuestLinkCaller(caller, remote);
            if (remote.Node == Node || !DistributionPending.TryCaptureGuestLinkConnection(remote, out var entry, out var connected))
            {
                Unlink(caller, remote);

                return;
            }
            var existing = FindGuestDistributionLink(
                entry,
                connected,
                caller,
                remote
            );
            if (existing is null)
            {
                Unlink(caller, remote);

                return;
            }
            var link = existing;
            if (link.UnlinkId is not null || link.Identity.PendingUnlink is not null)
                return;
            ulong unlinkId = ReserveDistributionUnlinkId();
            preparation = entry.Owner.PrepareGuestUnlinkOutput(link, unlinkId);
        }
        Task publication = await preparation.ConfigureAwait(false);
        // Cancel only the guest wait. A precommitted unlink belongs to the connection and global owned-output drain.
        await caller.WaitForDistributionControlCompletionAsync(
            publication.WaitAsync(caller.Cancellation), caller.Cancellation).ConfigureAwait(false);
    }

    internal bool GuestUnlinkStillCurrent(DistributionProcessLink link)
        => distributionProcessLinks.Contains(link) && link.UnlinkId is null && link.Identity.PendingUnlink is null;

    internal void CommitGuestUnlink(DistributionProcessLink link, ulong unlinkId, ProcessUnlinkIdentity marker)
    {
        link.UnlinkId = unlinkId;
        link.Identity.PendingUnlink = marker;
        link.Local.Links.Remove(link.Remote);
        link.Local.PriorityLinks.Remove(link.Remote);
        link.Local.PendingLinkOrigins.Remove(link.Identity);
    }

    private void ValidateGuestLinkCaller(ProcessContext caller, Pid remote)
    {
        caller.Cancellation.ThrowIfCancellationRequested();
        if (!IsOwnedLiveNodeProcess(caller) ||
            remote.LocalIdentity is not null && !ReferenceEquals(remote.LocalIdentity, LocalIdentity))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    private DistributionProcessLink? FindGuestDistributionLink(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        ProcessContext caller,
        Pid remote
    ) => distributionProcessLinks.LastOrDefault(link => ReferenceEquals(link.Entry, entry) &&
        ReferenceEquals(link.Connection, connected) && link.ConnectionId == entry.ConnectionId &&
        ReferenceEquals(link.Local, caller) && link.Remote.Equals(remote));

    private static void InstallGuestDistributionLink(DistributionProcessLink link, bool priority)
    {
        var caller = link.Local;
        caller.PendingLinkOrigins.Remove(link.Identity);
        caller.LinkIdentities[link.Remote] = link.Identity;
        caller.Links.Add(link.Remote);
        if (priority)
            caller.PriorityLinks.Add(link.Remote);
        else
            caller.PriorityLinks.Remove(link.Remote);
        link.Established = true;
    }

    private void RollBackUnpublishedGuestLink(DistributionProcessLink link)
    {
        // Retirement owns its queued noconnection; terminal cleanup owns any captured EXIT continuation.
        if (!IsOwnedLiveNodeProcess(link.Local) ||
            !link.Entry.Owner.IsConnectedSignalGeneration(link.Entry, link.Connection, link.ConnectionId))
            return;
        distributionProcessLinks.Remove(link);
        RemoveDistributionLinkIdentity(link);
    }
}
