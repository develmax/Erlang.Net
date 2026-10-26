namespace Erlang;

internal sealed class DistributionOutputSequencer : IDisposable
{
    private readonly object gate = new();
    private readonly LinkedList<DistributionOutputTicket> ordered = new();
    private readonly HashSet<object> owners = new(ReferenceEqualityComparer.Instance);
    private readonly CancellationTokenSource retired = new();
    private bool closed;

    internal DistributionOutputTicket? TryReserveImmediate(object producer)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (ordered.Count != 0)
                return null;

            return Reserve(producer);
        }
    }

    internal DistributionOutputTicket Reserve(object producer)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            if (!owners.Add(producer))
                throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.DuplicateOwner);
            var ticket = new DistributionOutputTicket(this, producer);
            ticket.Node = ordered.AddLast(ticket);
            if (ordered.Count == 1)
                ticket.Turn.TrySetResult();

            return ticket;
        }
    }

    internal async ValueTask Publish(
        DistributionOutputTicket ticket,
        Func<HostDistributionBuffer> encode,
        Func<HostDistributionBuffer, bool> admit,
        Func<HostDistributionBuffer, Task> capacity,
        CancellationToken cancellation,
        bool deferStart,
        Func<Task, CancellationToken, ValueTask>? wait = null
    )
    {
        lock (gate)
        {
            if (closed)
                throw new OperationCanceledException(DistributionOutputLifecycleDiagnostics.Retired, retired.Token);
            if (!ReferenceEquals(ticket.Owner, this) || Interlocked.Exchange(ref ticket.Started, 1) != 0)
                throw new InvalidOperationException(DistributionOutputLifecycleDiagnostics.ForeignReservation);
        }
        try
        {
            // A HOST caller may hold a foreign outer runtime gate. Re-enter validation on another turn.
            if (deferStart)
                await Task.Yield();
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation, retired.Token);
            await Wait(ticket.Turn.Task, stop.Token).ConfigureAwait(false);
            stop.Token.ThrowIfCancellationRequested();
            HostDistributionBuffer packet = encode();
            while (true)
            {
                stop.Token.ThrowIfCancellationRequested();
                if (admit(packet))
                    return;
                await Wait(capacity(packet), stop.Token).ConfigureAwait(false);
            }
        }
        finally
        {
            Release(ticket);
        }

        ValueTask Wait(Task completion, CancellationToken stop)
            => wait is null ? new ValueTask(completion.WaitAsync(stop)) : wait(completion, stop);
    }

    internal void Abandon(DistributionOutputTicket ticket)
    {
        if (Interlocked.CompareExchange(ref ticket.Started, 1, 0) == 0)
            Release(ticket);
    }

    private void Release(DistributionOutputTicket ticket)
    {
        lock (gate)
        {
            if (ticket.Released)
                return;
            ticket.Released = true;
            if (ticket.Node is { } node)
                ordered.Remove(node);
            ticket.Node = null;
            owners.Remove(ticket.Producer);
            ordered.First?.Value.Turn.TrySetResult();
        }
    }

    public void Dispose()
    {
        DistributionOutputTicket[] abandoned;
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
            abandoned = ordered.ToArray();
        }
        retired.Cancel();
        foreach (var ticket in abandoned)
            Abandon(ticket);
    }
}
