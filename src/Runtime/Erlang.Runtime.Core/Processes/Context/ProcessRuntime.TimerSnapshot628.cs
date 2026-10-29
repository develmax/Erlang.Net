namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void SendTimerSnapshot628(
        Term destination,
        Term message,
        MailboxPublicationScope593 publication
    )
    {
        lock (gate)
        {
            if (IsHaltRequested)
                return;
            Term resolved = destination is Atom name ? WhereIs(name.Name) : destination;
            if (resolved is Pid target && processes.TryGetValue(target, out var receiver))
                receiver.Mailbox.SendTracedSnapshot593(
                    message,
                    null,
                    preserve: true,
                    from: Term.A(ProcessTraceNames.ClockService),
                    publication593: publication
                );
        }
    }
}
