namespace Erlang;

internal sealed partial class RuntimeTimers
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot)
    {
        var captured = new List<(Term Destination, Term Message, IMailboxRootInterest593? Interest)>();
        try
        {
            lock (gate)
                foreach (var timer in timers.Values)
                    captured.Add((timer.Destination, timer.Message, (IMailboxRootInterest593?)timer.Payload.Graph?.Retain()));
            foreach (var timer in captured)
                snapshot.Graph(
                    timer.Message,
                    NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Process, timer.Destination),
                    NodeDistReferenceNames.Timer
                );
        }
        finally
        {
            foreach (var timer in captured) MailboxRootSettlement593.Settle(timer.Interest?.ReleaseLast());
        }
    }
}
