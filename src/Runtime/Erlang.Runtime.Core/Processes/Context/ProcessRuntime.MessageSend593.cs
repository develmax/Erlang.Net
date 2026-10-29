namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Snapshot and process_info origins share explicit recipient custody.
    internal bool MessageSnapshotRouteReady593 => true;

    internal bool CanBeginMessageSnapshot593(ProcessContext sender, Term target)
    {
        lock (gate) return ResolveMessageSnapshot593(
            sender,
            target,
            out _,
            out _
        );
    }

    private bool ResolveMessageSnapshot593(
        ProcessContext sender,
        Term target,
        out Pid destination,
        out ProcessContext? receiver
    )
    {
        destination = sender.Self;
        receiver = null;
        if (!MessageSnapshotRouteReady593 || !ReferenceEquals(sender.Runtime, this) || sender.Terminated || IsHaltRequested
            || sender.SequentialTrace.Token is not null || !ProcessTracing.SupportsMessageSnapshots593())
            return false;

        if (target is Pid pid && pid.Node == Node)
            destination = pid;
        else if (target is Atom name && names.TryGetValue(name.Name, out Term? registered) && registered is Pid named)
            destination = named;
        else
            return false;

        processes.TryGetValue(destination, out receiver);

        return true;
    }

    // Eligibility is RECHECKED under R before the FIRST trace or message output.
    // Any unsupported alias/port/distribution/pattern/sequential state returns
    // false without publication, allowing old whole-send fallback outside R.
    internal bool TrySendMessageSnapshot593(ProcessContext sender, PreparedSendArguments593 outgoing)
    {
        lock (gate)
        {
            if (!ResolveMessageSnapshot593(
                sender,
                outgoing.Target,
                out Pid destination,
                out ProcessContext? receiver
            ))
                return false;

            sender.SavedCalls.Sent();
            ProcessTracing.SentSnapshot593(
                sender,
                outgoing.Target,
                outgoing.Message,
                receiver is not null,
                outgoing.Publication593
            );
            if (receiver is not null)
            {
                SequenceTraceToken? token = SequenceTracing.Sent(sender, destination, outgoing.Message);
                receiver.Mailbox.SendTracedSnapshot593(
                    outgoing.Message,
                    token,
                    from: sender.Self,
                    publication593: outgoing.Publication593
                );
            }

            return true;
        }
    }
}
