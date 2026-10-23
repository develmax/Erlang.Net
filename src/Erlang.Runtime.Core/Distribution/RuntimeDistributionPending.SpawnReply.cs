namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void RouteConnectedSpawnReply(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        TupleTerm control
    )
    {
        Term rawToken = ((Integer)control.Items[0]).Value == DistributionControlOpcodes.SpawnReplyTrace
            ? control.Items[DistributionSpawnLayout.ReplyTokenIndex] : Nil.Value;
        var trace = DistributionSequenceTraceToken.Decode(rawToken);
        var reference = (ReferenceTerm)control.Items[DistributionSpawnLayout.ReferenceIndex];
        var owner = (Pid)control.Items[DistributionSpawnLayout.ParentIndex];
        int flags = (int)((Integer)control.Items[DistributionSpawnLayout.ReplyFlagsIndex]).Value;
        Term result = control.Items[DistributionSpawnLayout.ReplyResultIndex];
        if (reference.Node == runtime.Node && !runtime.IsCurrentLocal(reference) ||
            owner.Node == runtime.Node && !runtime.IsCurrentLocal(owner))
            return;
        if (!runtime.IsCurrentLocal(reference) || !runtime.IsCurrentLocal(owner) ||
            result is Pid child && (child.LocalIdentity is not null || child.Node != entry.Node.Name || child.Creation != connected.RemoteCreation))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (!connectedSpawnRequests.TryGetValue(reference, out var request) ||
            !ReferenceEquals(request.Entry, entry) || !ReferenceEquals(request.Connection, connected) ||
            request.ConnectionId != entry.ConnectionId || !request.Owner.Self.Equals(owner) || request.Replied)
            return;
        request.Replied = true;
        request.Child = result as Pid;
        request.ReplyFlags = flags;
        if (request.Abandoned || request.OwnerExitReason is not null)
        {
            connectedSpawnRequests.Remove(reference);
            CleanupUndeliveredSpawnReply(request);

            return;
        }
        if (result is Atom)
        {
            connectedSpawnRequests.Remove(reference);
            if (request.Options.ErrorReply)
                request.Owner.Mailbox.SendTraced(
                    Term.Tuple(
                        request.Options.Tag,
                        reference,
                        Term.A(ProcessSpawnRequestNames.Error),
                        result
                    ),
                    trace.Token,
                    trace.Preserve,
                    from: entry.Node
                );

            return;
        }
        runtime.InstallConnectedSpawnReplyRelations(request, (Pid)result, flags);
        request.Owner.Mailbox.SendPending(
            Term.Tuple(
                request.Options.Tag,
                reference,
                Term.A(ProcessSpawnRequestNames.Success),
                result
            ),
            () =>
            {
                lock (gate)
                {
                    if (!connectedSpawnRequests.Remove(reference, out var current) || !ReferenceEquals(current, request))
                        return null;

                    return request.Options.SuccessReply ? false : null;
                }
            },
            trace.Token,
            trace.Preserve,
            from: result
        );
    }

    internal bool IsPendingConnectedSpawn(ProcessContext owner, ReferenceTerm reference)
    {
        lock (gate)
            return connectedSpawnRequests.TryGetValue(reference, out var request) && ReferenceEquals(request.Owner, owner);
    }

    internal bool AbandonConnectedSpawnRequest(ProcessContext owner, ReferenceTerm reference)
    {
        lock (gate)
        {
            if (!connectedSpawnRequests.TryGetValue(reference, out var request) ||
                !ReferenceEquals(request.Owner, owner) || request.Abandoned)
                return false;
            request.Abandoned = true;
            if (request.Replied)
            {
                connectedSpawnRequests.Remove(reference);
                runtime.RemoveConnectedSpawnReplyRelations(request);
                CleanupUndeliveredSpawnReply(request);
            }

            return true;
        }
    }

    private void CleanupUndeliveredSpawnReply(DistributionSpawnRequest request)
    {
        if (request.Child is not { } child ||
            !IsConnectedSignalGeneration(request.Entry, request.Connection, request.ConnectionId))
            return;
        if ((request.ReplyFlags & DistributionSpawnLayout.MonitorFlag) != 0 &&
            (request.Connection.Flags & DistributionPassThroughFlags.ProcessMonitors) == DistributionPassThroughFlags.ProcessMonitors)
            ScheduleConnectedControlSignal(
                request.Entry,
                request.Connection,
                request.ConnectionId,
                Term.Tuple(
                    Term.I(DistributionControlOpcodes.Demonitor),
                    request.Owner.Self,
                    child,
                    request.Reference
                ),
                request.Owner.Self
            );
        if ((request.ReplyFlags & DistributionSpawnLayout.LinkFlag) != 0)
            ScheduleConnectedControlSignal(
                request.Entry,
                request.Connection,
                request.ConnectionId,
                Term.Tuple(
                    Term.I(DistributionControlOpcodes.Exit),
                    request.Owner.Self,
                    child,
                    request.OwnerExitReason ?? Term.A(ProcessSpawnRequestNames.Abandoned)
                ),
                request.Owner.Self
            );
    }
}
