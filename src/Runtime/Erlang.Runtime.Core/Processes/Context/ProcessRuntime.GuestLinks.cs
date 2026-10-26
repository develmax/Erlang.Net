namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask LinkGuestAsync(ProcessContext caller, Pid remote, bool priority = false)
    {
        ValueTask<Task> preparation;
        bool asynchronous;
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
                InstallGuestDistributionLink(existing, priority);

                return;
            }
            var link = new DistributionProcessLink(
                entry,
                connected,
                entry.ConnectionId,
                caller,
                remote
            );
            asynchronous = caller.AsyncDistribution;
            preparation = entry.Owner.PrepareGuestLinkOutput(link, priority);
        }
        Task publication = await preparation.ConfigureAwait(false);
        if (!asynchronous)
        {
            // Accepted LINK belongs to the connection, even when killing its caller cancels this wait.
            await caller.WaitForDistributionControlCompletionAsync(
                publication.WaitAsync(caller.Cancellation), caller.Cancellation).ConfigureAwait(false);
        }
    }

    internal bool UseExistingGuestLink(DistributionProcessLink prepared, bool priority)
    {
        var existing = FindGuestDistributionLink(
            prepared.Entry,
            prepared.Connection,
            prepared.Local,
            prepared.Remote
        );
        if (existing is null || existing.UnlinkId is not null || existing.Identity.PendingUnlink is not null)
            return false;
        InstallGuestDistributionLink(existing, priority);

        return true;
    }

    internal void CommitGuestLink(DistributionProcessLink link, bool priority)
    {
        distributionProcessLinks.Add(link);
        InstallGuestDistributionLink(link, priority);
    }

    internal void ReserveGuestLinkCommit(DistributionProcessLink link)
    {
        distributionProcessLinks.EnsureCapacity(checked(distributionProcessLinks.Count + 1));
        link.Local.LinkIdentities.EnsureCapacity(checked(link.Local.LinkIdentities.Count + 1));
        link.Local.Links.EnsureCapacity(checked(link.Local.Links.Count + 1));
        link.Local.PriorityLinks.EnsureCapacity(checked(link.Local.PriorityLinks.Count + 1));
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

}
