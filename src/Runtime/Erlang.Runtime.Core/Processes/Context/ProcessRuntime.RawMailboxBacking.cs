namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool TrySendBackedRawMailbox(ProcessContext recipient, Term message, bool priority)
    {
        if (System.Threading.Monitor.IsEntered(gate) || recipient.Mailbox.DeferredGateHeld)
            return false;
        lock (gate)
        {
            // Preserve legacy close/shutdown behavior. This hook never changes
            // runtime admission policy or activates execution from message data.
            if (disposed || IsHaltRequested || recipient.Terminated || recipient.Mailbox.IsClosedForBacking
                || !processes.TryGetValue(recipient.Self, out var actual)
                || !ReferenceEquals(actual, recipient))
                return false;
        }
        try
        {
            return TrySendBackedLocalAsync(recipient.Self, message, priority: priority)
                .GetAwaiter().GetResult();
        }
        catch (ObjectDisposedException)
        {
            // Preparation independently settled rollback before throwing. A
            // closed actual destination historically drops Mailbox.Send.
            // Otherwise preserve the actual failure for the caller.
            lock (gate)
            {
                if (recipient.Terminated || recipient.Mailbox.IsClosedForBacking)
                    return false;
                if (disposed || IsHaltRequested)
                    return false;
            }
            throw;
        }
    }
}
