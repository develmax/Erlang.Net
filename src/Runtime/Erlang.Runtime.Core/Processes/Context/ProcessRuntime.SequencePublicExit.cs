namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void Signal(
        ProcessContext target,
        Pid sender,
        Term reason,
        bool explicitSignal,
        RuntimeProcessPayload641? reason641,
        PreparedSequenceTermination? sequence641
    )
    {
        bool kill = explicitSignal && reason.Equals(Term.A(ProcessExitSignals.Kill));
        if (target.TrapExits && !kill)
        {
            var publication641 = RuntimeProcessPayload641.CombinePublication641(
                this,
                null,
                Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender, reason),
                reason641,
                null
            );
            try
            {
                target.Mailbox.SendTracedSnapshot593(
                    publication641.Value641,
                    null,
                    from: sender,
                    publication593: publication641.Publication641
                );
            }
            finally
            {
                publication641.Close641();
            }

            return;
        }
        if (!kill && reason.Equals(Term.A(ProcessExitReasons.Normal)) && !sender.Equals(target.Self))
            return;
        Terminate(
            target,
            kill ? Term.A(ProcessExitReasons.Killed) : reason,
            kill ? null : reason641,
            null,
            sequence641
        );
    }

    private void SendPreparedExitCore641(
        ProcessContext sender,
        Term destination,
        Term reason,
        bool legacy,
        bool priority,
        RuntimeProcessPayload641 complete641,
        PreparedSequenceTermination? sequence641
    )
    {
        // complete641 contains {destination,reason}; callbacks use the detached reason above.
        if (destination is PortTerm port)
        {
            Ports.SendExit641(
                sender,
                port,
                reason,
                complete641
            );

            return;
        }
        if (destination is not Pid && destination is not ReferenceTerm)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            if (destination is Pid pid)
            {
                if (!processes.TryGetValue(pid, out var target))
                    return;
                bool normalKills = legacy && pid.Equals(sender.Self);
                if (normalKills && (reason.Equals(Term.A(ProcessExitSignals.Kill)) || !target.TrapExits))
                {
                    Terminate(
                        target,
                        reason.Equals(Term.A(ProcessExitSignals.Kill)) ? Term.A(ProcessExitReasons.Killed) : reason,
                        reason.Equals(Term.A(ProcessExitSignals.Kill)) ? null : complete641,
                        null,
                        sequence641
                    );
                    sender.Cancellation.ThrowIfCancellationRequested();

                    return;
                }
                QueueExit(
                    target,
                    sender.Self,
                    reason,
                    true,
                    normalKills,
                    false,
                    null,
                    false,
                    reason641: complete641
                );
            }
            else if (destination is ReferenceTerm reference && aliases.TryGetValue(reference, out var alias) && processes.TryGetValue(alias.Owner, out var target))
                QueueExit(
                    target,
                    sender.Self,
                    reason,
                    true,
                    false,
                    priority,
                    reference,
                    false,
                    reason641: complete641
                );
        }
    }

}
