namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void SendTimerSnapshot628(
        Term destination,
        Term message,
        MailboxPublicationScope593 publication
    )
    {
        // Fire already owns the admitted timer payload/publication and calls
        // here outside R. The prepared destination graph takes its own actual
        // interests before the timer callback releases that source scope.
        if (TrySendBackedLocalAsync(
            destination,
            message,
            from: Term.A(ProcessTraceNames.ClockService),
            preserve: true
        )
            .GetAwaiter().GetResult())
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
