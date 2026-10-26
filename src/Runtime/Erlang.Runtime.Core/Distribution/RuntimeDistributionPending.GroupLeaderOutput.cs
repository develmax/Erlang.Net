namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal async ValueTask<bool> PublishConnectedGroupLeaderAsync(
        ProcessContext producer,
        Pid leader,
        Pid remote
    )
    {
        RuntimeDistributionPendingEntry entry;
        DistributionConnectedEntry connected;
        uint connectionId;
        lock (gate)
        {
            producer.Cancellation.ThrowIfCancellationRequested();
            RequireProcess(producer);
            if (!entries.TryGetValue(remote.Node, out var weak) || !weak.TryGetTarget(out var existing) ||
                existing.Connected is not { } connection ||
                !IsConnectedSignalGeneration(existing, connection, existing.ConnectionId))
                return false;
            entry = existing;
            connected = connection;
            connectionId = entry.ConnectionId;
        }

        return await PublishLiveControl(
            entry,
            connected,
            connectionId,
            producer,
            Term.Tuple(Term.I(DistributionControlOpcodes.GroupLeader), leader, remote),
            producer.Self
        ).ConfigureAwait(false);
    }
}
