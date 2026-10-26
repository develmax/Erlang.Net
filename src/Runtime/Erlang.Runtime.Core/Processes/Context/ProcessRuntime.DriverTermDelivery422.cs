namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal int DeliverDriverTerm422(Pid recipient, byte[] encoded, Term? sender)
    {
        lock (gate)
        {
            if (disposed || IsHaltRequested || !IsCurrentLocal(recipient)
                || !processes.TryGetValue(recipient, out var receiver) || !IsOwnedLiveNodeProcess(receiver)) return 0;
            Term message = ImportExternalTerm(encoded).Term;
            receiver.Mailbox.SendTraced(
                message,
                null,
                preserve: true,
                from: sender
            );

            return 1;
        }
    }
}
