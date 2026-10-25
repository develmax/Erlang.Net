namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RemoveConnectedSpawnReplyRelations(DistributionSpawnRequest request)
    {
        lock (gate)
        {
            monitors.Remove(request.Reference);
            aliases.Remove(request.Reference);
            distributionProcessMonitors.RemoveAll(item => item.Origin &&
                ReferenceEquals(item.Local, request.Owner) && item.Reference.Equals(request.Reference));
            foreach (var link in distributionProcessLinks.Where(item => ReferenceEquals(item.Local, request.Owner) &&
                ReferenceEquals(item.Entry, request.Entry) && ReferenceEquals(item.Connection, request.Connection) &&
                item.Remote.Equals(request.Child)).ToArray())
            {
                // Suppress the already queued establishment callback without sending an UNLINK before EXIT.
                link.UnlinkId = DistributionSpawnNames.AbandonedLinkMarker;
                distributionProcessLinks.Remove(link);
                RemoveDistributionLinkIdentity(link);
            }
        }
    }

    internal void InstallConnectedSpawnReplyRelations(DistributionSpawnRequest request, Pid child, int flags)
    {
        lock (gate)
        {
            if (!IsOwnedLiveNodeProcess(request.Owner))
                return;
            if ((flags & DistributionSpawnLayout.LinkFlag) != 0)
            {
                // The spawn reply already establishes this relation before parent termination can observe it.
                var link = new DistributionProcessLink(
                    request.Entry,
                    request.Connection,
                    request.ConnectionId,
                    request.Owner,
                    child
                )
                {
                    Established = true
                };
                distributionProcessLinks.Add(link);
                request.Owner.Links.Add(child);
                request.Owner.LinkIdentities.Add(child, link.Identity);
                if (request.Options.Spawn.PriorityLink)
                    request.Owner.PriorityLinks.Add(child);
            }
            if ((flags & DistributionSpawnLayout.MonitorFlag) != 0)
            {
                InstallConnectedSpawnMonitorOptions(request, child);
                distributionProcessMonitors.Add(new(
                    request.Entry,
                    request.Connection,
                    request.ConnectionId,
                    request.Owner,
                    child,
                    child,
                    request.Reference,
                    true
                )
                { Established = true, SpawnOptions = true });
            }
        }
    }

    internal Pid CreateConnectedSpawn(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        ReferenceTerm reference,
        Pid parent,
        Pid groupLeader,
        TupleTerm mfa,
        Term arguments,
        ProcessSpawnOptions options,
        Term wireOptions,
        SequenceTraceToken? requestToken,
        out SequenceTraceToken? replyToken
    )
    {
        lock (gate)
        {
            if (!entry.Owner.IsConnectedSignalGeneration(entry, connected, entry.ConnectionId))
                throw new InvalidOperationException(DistributionConnectedDiagnostics.Connected);
            if (options.Monitor && distributionProcessMonitors.Any(item => !item.Origin &&
                ReferenceEquals(item.Entry, entry) && ReferenceEquals(item.Connection, connected) &&
                item.Reference.Equals(reference)))
                throw new InvalidOperationException(DistributionSpawnDiagnostics.DuplicateMonitor);
            var values = Cons.Items(arguments).ToArray();
            var module = (Atom)mfa.Items[0];
            var function = (Atom)mfa.Items[1];
            var handle = SpawnTraced(
                child =>
                {
                    if (requestToken is not null)
                        SequenceTracing.Received(
                            child,
                            arguments,
                            requestToken,
                            false
                        );

                    return child.Runtime.Modules.Call(
                        child,
                        module.Name,
                        function.Name,
                        values
                    );
                },
                null,
                false,
                new(mfa, arguments, Nil.Value),
                initialAsyncDistribution: options.AsyncDistribution,
                initialPriority: options.Priority,
                schedulerBinding: options.ResolveScheduler(ActorDispatchCapacity),
                initialFullSweepAfter: options.FullSweepAfter,
                initialMessageQueueData: options.MessageQueueData
            );
            // The actor first takes this same gate; none of its guest body can run yet.
            var context = processes[handle.Pid];
            context.GroupLeader = groupLeader;
            if (options.Link)
            {
                var link = new DistributionProcessLink(
                    entry,
                    connected,
                    entry.ConnectionId,
                    context,
                    parent
                )
                {
                    Established = true
                };
                distributionProcessLinks.Add(link);
                context.Links.Add(parent);
                context.LinkIdentities.Add(parent, link.Identity);
            }
            if (options.Monitor)
                distributionProcessMonitors.Add(new(
                    entry,
                    connected,
                    entry.ConnectionId,
                    context,
                    parent,
                    context.Self,
                    reference,
                    false
                )
                { Established = true });

            replyToken = SequenceTracing.ConnectedSpawnCreated(
                context,
                reference,
                parent,
                groupLeader,
                mfa,
                wireOptions,
                requestToken
            );

            return context.Self;
        }
    }
}
