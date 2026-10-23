namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ReferenceTerm ReserveConnectedSpawnReference(ProcessContext owner, ProcessSpawnOptions options)
    {
        lock (gate)
        {
            ReferenceTerm ordinary = MakeReference();
            if (!options.Monitor || options.MonitorOptions.AliasMode is null)
                return ordinary;
            ulong pid = (owner.Self.Id << ProcessAliasLayout.PidTagBits) | ProcessAliasLayout.PidTag;

            return new ReferenceTerm(
                LocalIdentity,
                [ordinary.Words[0], ordinary.Words[1] | ProcessAliasLayout.PidMarker, ordinary.Words[2],
                    (uint)pid, (uint)(pid >> ReferenceIdentityLayout.WordBits)]
            );
        }
    }

    private void InstallConnectedSpawnMonitorOptions(DistributionSpawnRequest request, Pid child)
    {
        var options = request.Options.Spawn.MonitorOptions;
        if (options.AliasMode is not null)
            aliases.Add(request.Reference, new(request.Owner.Self,
                options.AliasMode == ProcessMonitorOptionNames.ReplyDemonitor, false));
        monitors.Add(
            request.Reference,
            new(
                request.Owner.Self,
                child,
                child,
                options.Tag,
                options.Priority,
                options.AliasMode == ProcessAliasNames.Explicit
            )
        );
    }

    private void DetachConnectedSpawnMonitor(ProcessContext observer, ReferenceTerm reference)
    {
        var remote = distributionProcessMonitors.FirstOrDefault(item => item.Origin && item.SpawnOptions &&
            ReferenceEquals(item.Local, observer) && item.Reference.Equals(reference));
        if (remote is null)
            return;
        distributionProcessMonitors.Remove(remote);
        if ((remote.Connection.Flags & DistributionPassThroughFlags.ProcessMonitors) == DistributionPassThroughFlags.ProcessMonitors &&
            remote.Entry.Owner.IsConnectedSignalGeneration(remote.Entry, remote.Connection, remote.ConnectionId))
            remote.Entry.Owner.ScheduleConnectedControlSignal(
                remote.Entry,
                remote.Connection,
                remote.ConnectionId,
                Term.Tuple(
                    Term.I(DistributionControlOpcodes.Demonitor),
                    observer.Self,
                    remote.Remote,
                    reference
                ),
                observer.Self
            );
    }
}
