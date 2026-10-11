using System.Diagnostics;

namespace Erlang;

public sealed class Mailbox
{
    private readonly object gate = new();
    private readonly LinkedList<Entry> messages = new();
    private LinkedListNode<Entry>? lastPriority;
    private int pending;
    private TaskCompletionSource changed = NewSignal();
    internal Action? ProcessSignals { get; init; }

    private readonly record struct Entry(Term Value, Func<bool?>? Admission);

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Count
    {
        get
        {
            ProcessSignals?.Invoke();
            lock (gate)
                return messages.Count - pending;
        }
    }

    public void Send(Term message, bool priority = false)
    {
        lock (gate)
        {
            if (priority)
                lastPriority = lastPriority is null ? messages.AddFirst(new Entry(message, null)) : messages.AddAfter(lastPriority, new Entry(message, null));
            else
                messages.AddLast(new Entry(message, null));
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
    }

    internal void SendPending(Term message, Func<bool?> admission)
    {
        lock (gate)
        {
            messages.AddLast(new Entry(message, admission));
            pending++;
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
    }

    internal void AdmitPending()
    {
        lock (gate)
        {
            if (pending == 0)
                return;
            for (var node = messages.First; node is not null;)
            {
                var next = node.Next;
                if (node.Value.Admission is { } admission)
                {
                    bool? priority = admission();
                    pending--;
                    node.Value = new Entry(node.Value.Value, null);
                    if (priority is null)
                        RemoveNode(node);
                    else if (priority.Value)
                    {
                        messages.Remove(node);
                        if (lastPriority is null)
                            messages.AddFirst(node);
                        else
                            messages.AddAfter(lastPriority, node);
                        lastPriority = node;
                    }
                }
                node = next;
            }
        }
    }

    internal void DiscardPending()
    {
        lock (gate)
        {
            if (pending == 0)
                return;
            for (var node = messages.First; node is not null;)
            {
                var next = node.Next;
                if (node.Value.Admission is not null)
                    RemoveNode(node);
                node = next;
            }
            pending = 0;
        }
    }

    public int Remove(Func<Term, bool> predicate)
    {
        ProcessSignals?.Invoke();
        lock (gate)
        {
            int count = 0;
            for (var n = messages.First; n != null;)
            {
                var next = n.Next;
                if (n.Value.Admission is null && predicate(n.Value.Value))
                {
                    RemoveNode(n);
                    count++;
                }
                n = next;
            }

            return count;
        }
    }

    public async ValueTask<T?> ReceiveAsync<T>(Func<Term, T?> select, TimeSpan? timeout, CancellationToken cancellation = default) where T : class
    {
        if (timeout < TimeSpan.Zero)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        long start = Stopwatch.GetTimestamp();
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            ProcessSignals?.Invoke();
            Task signal;
            bool retry;
            lock (gate)
            {
                retry = pending != 0;
                for (var node = retry ? null : messages.First; node != null; node = node.Next)
                {
                    var match = select(node.Value.Value);
                    if (match is not null)
                    {
                        RemoveNode(node);

                        return match;
                    }
                }
                signal = changed.Task;
            }
            if (retry)
                continue;
            TimeSpan? remaining = timeout - Stopwatch.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero)
                return null;
            try
            {
                if (remaining is { } finite)
                    await signal.WaitAsync(finite, cancellation);
                else
                    await signal.WaitAsync(cancellation);
            }
            catch (TimeoutException)
            {
                return null;
            }
        }
    }

    private void RemoveNode(LinkedListNode<Entry> node)
    {
        if (ReferenceEquals(node, lastPriority))
            lastPriority = node.Previous;
        messages.Remove(node);
    }
}
