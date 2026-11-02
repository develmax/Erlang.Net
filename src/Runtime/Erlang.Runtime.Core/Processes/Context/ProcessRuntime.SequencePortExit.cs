namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void QueuePortBadSignalWithSequence(PortTerm identity, Pid connected)
    {
        lock (gate)
        {
            if (!processes.TryGetValue(connected, out ProcessContext? process)) return;
            Term reason = Term.A(PortNames.BadSignal);
            var terminal = CreatePendingSequenceTerminationControl(process, reason,
                () => !process.Terminated && !process.TrapExits);
            Func<bool?> admission = () =>
            {
                if (process.Terminated) return null;
                if (process.TrapExits) return false;
                Terminate(
                    process,
                    reason,
                    null,
                    null,
                    terminal.Prepared
                );

                return null;
            };
            RegisterPendingSequenceTermination(admission, terminal);
            process.DeferredSignalDrain ??= new();
            process.Mailbox.SendPendingSnapshot593(
                Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), identity, reason),
                admission,
                prepareBacking: true
            );
        }
    }
}
