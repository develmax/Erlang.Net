namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void QueueExitWithSequenceControl(
        ProcessContext target,
        Pid sender,
        Term reason,
        bool explicitSignal,
        bool normalKills,
        bool priority,
        ReferenceTerm? alias,
        bool linked,
        SequenceTraceToken? token = null,
        RuntimeProcessPayload641? reason641 = null,
        PreparedMailboxMessage? preparedMessage = null
    )
    {
        bool kill = explicitSignal && reason is Atom { Name: ProcessExitSignals.Kill };
        Term effectiveReason = kill ? Term.A(ProcessExitReasons.Killed) : reason;
        var terminal = CreatePendingSequenceTerminationControl(
            target,
            effectiveReason,
            WillTerminate,
            linked ? sender : null
        );
        Func<bool?> admission = Admit;
        RegisterPendingSequenceTermination(admission, terminal);
        Term value = preparedMessage?.Value ?? Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender, reason);
        var publication641 = RuntimeProcessPayload641.CombinePublication641(
            this,
            null,
            value,
            reason641,
            null
        );
        MailboxPublicationScope593? physical = preparedMessage is null ? null
            : MailboxPublicationScope593.Forward(this, preparedMessage.Graph);
        try
        {
            target.DeferredSignalDrain ??= new();
            target.Mailbox.SendPendingSnapshot593(
                value,
                admission,
                token: preparedMessage?.Token ?? token,
                from: sender,
                publication593: physical ?? publication641.Publication641,
                prepareBacking: true
            );
        }
        finally
        {
            physical?.Dispose();
            publication641.Close641();
        }

        bool WillTerminate()
        {
            if (target.Terminated) return false;
            if (alias is not null && (!aliases.TryGetValue(alias, out var permission) || !permission.Owner.Equals(target.Self))) return false;
            if (linked && !target.Links.Contains(sender)) return false;

            return (kill || !target.TrapExits)
                && (kill || reason is not Atom { Name: ProcessExitReasons.Normal } || normalKills);
        }

        bool? Admit()
        {
            if (target.Terminated) return null;
            bool prioritized = false;
            if (alias is not null)
            {
                if (!aliases.TryGetValue(alias, out var permission) || !permission.Owner.Equals(target.Self)) return null;
                prioritized = priority && permission.Priority;
            }
            if (linked)
            {
                target.LinkIdentities.Remove(sender);
                if (!target.Links.Remove(sender)) return null;
                prioritized = target.PriorityLinks.Remove(sender);
            }
            if (target.TrapExits && !kill) return prioritized;
            if (!kill && reason is Atom { Name: ProcessExitReasons.Normal } && !normalKills) return null;
            Terminate(
                target,
                effectiveReason,
                kill ? null : reason641,
                null,
                terminal.Prepared
            );

            return null;
        }
    }
}
