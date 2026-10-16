namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private void RouteConnectedSpawnRequest(
        RuntimeDistributionPendingEntry entry,
        DistributionConnectedEntry connected,
        TupleTerm control,
        Term arguments
    )
    {
        SequenceTraceToken? requestToken = ((Integer)control.Items[0]).Value == DistributionControlOpcodes.SpawnRequestTrace
            ? DistributionSequenceTraceToken.Decode(control.Items[DistributionSpawnLayout.RequestTokenIndex]).Token : null;
        SequenceTraceToken? replyToken;
        var reference = (ReferenceTerm)control.Items[DistributionSpawnLayout.ReferenceIndex];
        var parent = (Pid)control.Items[DistributionSpawnLayout.ParentIndex];
        if (parent.LocalIdentity is not null || parent.Node != entry.Node.Name || parent.Creation != connected.RemoteCreation ||
            reference.LocalIdentity is not null || reference.Node != entry.Node.Name || reference.Creation != connected.RemoteCreation)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term result;
        int replyFlags = 0;
        try
        {
            var options = ProcessSpawnOptions.Parse(control.Items[DistributionSpawnLayout.OptionsIndex]);
            if (options.Monitor && (connected.Flags & DistributionPassThroughFlags.ProcessMonitors) != DistributionPassThroughFlags.ProcessMonitors)
                throw new NotSupportedException(DistributionSpawnDiagnostics.SenderOptionsNotImplemented);
            result = runtime.CreateConnectedSpawn(
                entry,
                connected,
                reference,
                parent,
                (Pid)control.Items[DistributionSpawnLayout.GroupLeaderIndex],
                (TupleTerm)control.Items[DistributionSpawnLayout.MfaIndex],
                arguments,
                options,
                control.Items[DistributionSpawnLayout.OptionsIndex],
                requestToken,
                out replyToken
            );
            replyFlags = (options.Link ? DistributionSpawnLayout.LinkFlag : 0) |
                (options.Monitor ? DistributionSpawnLayout.MonitorFlag : 0);
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            result = Term.A(ProcessSpawnOptionNames.BadOption);
            replyToken = runtime.SequenceTracing.ConnectedSpawnFailed(
                reference,
                parent,
                (Pid)control.Items[DistributionSpawnLayout.GroupLeaderIndex],
                (TupleTerm)control.Items[DistributionSpawnLayout.MfaIndex],
                control.Items[DistributionSpawnLayout.OptionsIndex],
                result,
                requestToken
            );
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)))
        {
            result = error.Reason;
            replyToken = runtime.SequenceTracing.ConnectedSpawnFailed(
                reference,
                parent,
                (Pid)control.Items[DistributionSpawnLayout.GroupLeaderIndex],
                (TupleTerm)control.Items[DistributionSpawnLayout.MfaIndex],
                control.Items[DistributionSpawnLayout.OptionsIndex],
                result,
                requestToken
            );
        }
        // Reserve output under the shared gate before the newly queued actor can emit exit/DOWN.
        ScheduleConnectedControlSignal(
            entry,
            connected,
            entry.ConnectionId,
            replyToken is null
                ? Term.Tuple(
                    Term.I(DistributionControlOpcodes.SpawnReply),
                    reference,
                    parent,
                    Term.I(replyFlags),
                    result
                )
                : Term.Tuple(
                    Term.I(DistributionControlOpcodes.SpawnReplyTrace),
                    reference,
                    parent,
                    Term.I(replyFlags),
                    result,
                    replyToken.ToTerm()
                ),
            result as Pid ?? connected.Controller.Self
        );
    }
}
