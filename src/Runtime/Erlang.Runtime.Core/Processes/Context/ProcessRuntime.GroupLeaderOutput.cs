namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<bool> SetGroupLeaderAsync(
        ProcessContext caller,
        Pid leader,
        Pid target
    )
    {
        ValueTask<bool> publication;
        lock (gate)
        {
            caller.Cancellation.ThrowIfCancellationRequested();
            if (!IsOwnedLiveNodeProcess(caller) ||
                leader.LocalIdentity is not null && !ReferenceEquals(leader.LocalIdentity, LocalIdentity) ||
                target.LocalIdentity is not null && !ReferenceEquals(target.LocalIdentity, LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (target.Node == Node)
                return SetGroupLeader(leader, target);

            // Reserve publication under the same gate as local ownership validation.
            publication = DistributionPending.PublishConnectedGroupLeaderAsync(caller, leader, target);
        }
        _ = await publication.ConfigureAwait(false);

        // Acceptance does not promise that a remote target exists or survives disconnect.
        return true;
    }
}
