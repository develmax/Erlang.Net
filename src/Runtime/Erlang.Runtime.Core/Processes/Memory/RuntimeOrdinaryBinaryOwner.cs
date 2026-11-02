namespace Erlang;

internal sealed class RuntimeOrdinaryBinaryOwner
{
    private readonly object gate = new();
    private readonly HashSet<OrdinaryBinaryAllocationTicket> pending = new();
    private bool closed;

    internal bool AdmissionClosed
    {
        get
        {
            lock (gate) return closed;
        }
    }

    internal OrdinaryBinaryAllocationTicket Reserve()
    {
        var ticket = new OrdinaryBinaryAllocationTicket(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            pending.Add(ticket);
        }

        return ticket;
    }

    internal void Forget(OrdinaryBinaryAllocationTicket ticket)
    {
        lock (gate) pending.Remove(ticket);
    }

    internal void Stop()
    {
        lock (gate) closed = true;
    }

    internal Task Drain()
    {
        lock (gate)
        {
            closed = true;

            return Task.WhenAll(pending.Select(ticket => ticket.Completion));
        }
    }
}
