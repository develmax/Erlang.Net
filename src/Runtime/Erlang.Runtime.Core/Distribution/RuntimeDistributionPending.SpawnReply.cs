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
        var terminal641 = request.OwnerExitCustody641?.Borrow641(null);
        var dispatch641 = request.Abandoned || request.OwnerExitReason is not null
                ? null : request.MonitorCustody641?.Borrow641(null);
        try
        {
            request.Replied = true;
            request.Child = result as Pid;
            request.ReplyFlags = flags;
            if (request.Abandoned || request.OwnerExitReason is not null)
            {
                RemoveConnectedSpawnRequest641(reference);
                CleanupUndeliveredSpawnReply(request, terminal641);

                return;
            }
            if (result is Atom)
            {
                RemoveConnectedSpawnRequest641(reference);
                if (request.Options.ErrorReply)
                    runtime.SendMonitorPayload641(
                        request.Owner,
                        Term.Tuple(
                            dispatch641 is null ? request.Options.Tag : ((TupleTerm)dispatch641.Value641).Items[0],
                            reference,
                            Term.A(ProcessSpawnRequestNames.Error),
                            result
                        ),
                        null,
                        dispatch641,
                        token: trace.Token,
                        preserve: trace.Preserve,
                        from: entry.Node
                    );

                return;
            }
            runtime.InstallConnectedSpawnReplyRelations(request, (Pid)result, flags);
            runtime.SendMonitorPayload641(
                request.Owner,
                Term.Tuple(
                    dispatch641 is null ? request.Options.Tag : ((TupleTerm)dispatch641.Value641).Items[0],
                    reference,
                    Term.A(ProcessSpawnRequestNames.Success),
                    result
                ),
                () =>
                {
                    lock (gate)
                    {
                        if (!RemoveConnectedSpawnRequest641(reference, out var current) || !ReferenceEquals(current, request))
                            return null;

                        return request.Options.SuccessReply ? false : null;
                    }
                },
                dispatch641,
                token: trace.Token,
                preserve: trace.Preserve,
                from: result
            );
        }
        finally
        {
            dispatch641?.Close641();
            terminal641?.Close641();
        }
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
            request.MonitorCustody641?.Close641();
            if (request.Replied)
            {
                var terminal641 = request.OwnerExitCustody641?.Borrow641(null);
                try
                {
                    RemoveConnectedSpawnRequest641(reference);
                    runtime.RemoveConnectedSpawnReplyRelations(request);
                    CleanupUndeliveredSpawnReply(request, terminal641);
                }
                finally
                {
                    terminal641?.Close641();
                }
            }

            return true;
        }
    }

    private void CleanupUndeliveredSpawnReply(DistributionSpawnRequest request, RuntimeProcessPayload641? terminal641)
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
                request.Owner.Self,
                terminal641
            );
    }
}
