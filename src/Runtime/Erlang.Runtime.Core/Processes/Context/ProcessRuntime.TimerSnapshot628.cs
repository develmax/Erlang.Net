namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void SendTimerSnapshot628(
        Term destination,
        Term message,
        MailboxPublicationScope593 publication
    )
    {
        // The active timer publication owns the source while the destination
        // acquires its real graph. All preparation occurs outside R.
        if (TrySendBackedLocalAsync(
            destination,
            message,
            from: Term.A(ProcessTraceNames.ClockService),
            preserve: true
        ).GetAwaiter().GetResult())
            return;

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
