namespace Erlang;

public sealed partial class Mailbox
{
    internal NodeDistMailboxRead CaptureNodeDistReferences()
    {
        lock (gate)
        {
            var read = new NodeDistMailboxRead(messages.Count);
            try
            {
                foreach (Entry entry in messages)
                    if (!entry.Control) read.Add(entry.Value, entry.Custody593);
            }
            catch (Exception error)
            {
                read.Failure = error;
            }

            return read;
        }
    }
}
