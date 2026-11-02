namespace Erlang;

internal sealed partial class SequenceTracing
{
    internal async ValueTask<PreparedSequenceTraceOutput?> PrepareOutputAsync(Term[] sourceRoots, Term? tracee = null)
    {
        runtime.AssertSequenceTracePreparation();
        ProcessContext recipient;
        lock (gate)
        {
            // PIN erl_trace.c:981 captures the system tracer ONCE per output.
            // Later global replacement does not redirect or retry this event.
            if (Tracer() is not Pid pid || pid.Equals(tracee) || !processes.TryGetValue(pid, out recipient!)) return null;
        }
        while (true)
        {
            ProcessMessageQueueDataMode411 mode;
            long policy;
            lock (gate)
            {
                if (!processes.TryGetValue(recipient.Self, out ProcessContext? current)
                    || !ReferenceEquals(current, recipient)) return null;
                mode = recipient.Mailbox.MessageQueuePlacementMode;
                policy = recipient.Mailbox.MessageQueuePolicyRevision;
            }
            PreparedSequenceTraceOutput prepared;
            try
            {
                prepared = await runtime.MailboxRoots593.PrepareSequenceTraceOutputAsync(
                    runtime,
                    recipient,
                    mode,
                    policy,
                    sourceRoots
                ).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                lock (gate)
                {
                    if (!processes.TryGetValue(recipient.Self, out ProcessContext? current)
                        || !ReferenceEquals(current, recipient)) return null;
                    if (current.Mailbox.MessageQueuePlacementMode != mode
                        || current.Mailbox.MessageQueuePolicyRevision != policy) continue;
                }
                throw;
            }
            lock (gate)
            {
                // Death is an accepted drop at output; keep the real prepared
                // owner until normal disposal, never recapture another tracer.
                if (!processes.TryGetValue(recipient.Self, out ProcessContext? current)
                    || !ReferenceEquals(current, recipient)
                    || current.Mailbox.MessageQueuePlacementMode == mode
                        && current.Mailbox.MessageQueuePolicyRevision == policy) return prepared;
            }
            // A live policy change retries this SAME captured recipient outside
            // R. A null plan means captured suppression, never policy failure.
            await prepared.DisposeAsync().ConfigureAwait(false);
        }
    }

    // Final token/admission callers still revalidate their own state. Only the
    // captured output recipient's live storage policy can invalidate this plan.
    internal bool IsCurrentOutputPlan(PreparedSequenceTraceOutput? plan)
    {
        if (plan is null) return true;
        lock (gate)
        {
            if (!processes.TryGetValue(plan.Recipient.Self, out ProcessContext? current)
                || !ReferenceEquals(current, plan.Recipient)) return true;

            return current.Mailbox.MessageQueuePlacementMode == plan.Mode
                && current.Mailbox.MessageQueuePolicyRevision == plan.Policy;
        }
    }

    internal bool IsCurrentOutputPlanOrSuppressed(
        PreparedSequenceTraceOutput? plan,
        SequenceTraceToken token,
        SequenceTraceFlags kind,
        Term tracee
    )
    {
        lock (gate)
        {
            if ((token.Flags & kind) == 0 || plan is null) return true;

            return IsCurrentOutputPlan(plan);
        }
    }

    internal SequenceTraceToken? SentPrepared(
        ProcessContext sender,
        Term destination,
        Term message,
        PreparedSequenceTraceOutput? plan
    )
    {
        lock (gate)
        {
            SequenceTraceToken? token = Advance(sender);
            OutputPrepared(
                sender.Self,
                token,
                SequenceTraceFlags.Send,
                destination,
                message,
                plan
            );

            return token;
        }
    }

    private void OutputPrepared(
        Term tracee,
        SequenceTraceToken? token,
        SequenceTraceFlags kind,
        Term destination,
        Term message,
        PreparedSequenceTraceOutput? plan
    )
    {
        if (token is null || (token.Flags & kind) == 0 || plan is null)
            return;
        // Captured-none/self remains suppressed and a dead captured target is
        // dropped. No global tracer lookup or notification binding occurs here.
        if (!processes.TryGetValue(plan.Recipient.Self, out ProcessContext? current)
            || !ReferenceEquals(current, plan.Recipient)) return;
        if (!IsCurrentOutputPlan(plan))
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        Term notification = BuildOutputNotification(
            token,
            kind,
            destination,
            message
        );
        PreparedMailboxMessage prepared = plan!.BindNotification(notification);
        using var publication = MailboxPublicationScope593.Forward(runtime, prepared.Graph);
        plan.Recipient.Mailbox.SendTracedSnapshot593(
            prepared.Value,
            null,
            true,
            traceAdmission: false,
            publication593: publication
        );
    }

    private Term BuildOutputNotification(
        SequenceTraceToken token,
        SequenceTraceFlags kind,
        Term destination,
        Term message
    )
    {
        string type = kind switch
        {
            SequenceTraceFlags.Send => SequenceTraceNames.Send,
            SequenceTraceFlags.Receive => SequenceTraceNames.Receive,
            _ => SequenceTraceNames.PrintEvent
        };
        Term details = Term.Tuple(
            Term.A(type),
            Term.Tuple(Term.I(token.LastCount), Term.I(token.Serial)),
            token.Sender,
            destination,
            message
        );
        Term notification;
        if ((token.Flags & SequenceTraceFlags.Timestamp) != 0)
            notification = Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                token.Label,
                details,
                ErlangClockState.Timestamp(false)
            );
        else if ((token.Flags & SequenceTraceFlags.StrictMonotonicTimestamp) != 0)
            notification = Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                token.Label,
                details,
                Term.Tuple(
                    new Integer(ErlangClockState.MonotonicNanoseconds),
                    runtime.UniqueMonotonic436.Next(false)
                )
            );
        else if ((token.Flags & SequenceTraceFlags.MonotonicTimestamp) != 0)
            notification = Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                token.Label,
                details,
                new Integer(ErlangClockState.MonotonicNanoseconds)
            );
        else
            notification = Term.Tuple(Term.A(SequenceTraceNames.Message), token.Label, details);

        return notification;
    }
}
