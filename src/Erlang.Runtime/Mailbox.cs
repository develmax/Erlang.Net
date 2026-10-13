using System.Diagnostics;

namespace Erlang;

public sealed class Mailbox
{
    private readonly object gate = new();
    private readonly LinkedList<Entry> messages = new();
    private LinkedListNode<Entry>? lastPriority;
    private int pending;
    private int admittedCount;
    private TaskCompletionSource changed = NewSignal();
    internal Action? ProcessSignals { get; init; }
    internal Func<ValueTask<long>>? WaitForExecution { get; init; }
    internal Func<bool>? ExecutionBlocked { get; init; }
    internal Action? MessageReceived { get; init; }
    internal Action<Term, SequenceTraceToken?, bool>? TraceReceived { get; init; }
    internal Action? ReceiveTimedOut { get; init; }
    internal Action<bool>? WaitingStateChanged { get; init; }

    private readonly record struct Entry(
        Term Value,
        Func<bool?>? Admission,
        bool Control = false,
        SequenceTraceToken? TraceToken = null,
        bool PreserveTrace = false
    );

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void Pulse()
    {
        lock (gate)
        {
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
    }

    internal async ValueTask WaitForControlCompletion(Task completion, CancellationToken cancellation)
    {
        while (!completion.IsCompleted)
        {
            cancellation.ThrowIfCancellationRequested();
            Task changedTask;
            lock (gate)
                changedTask = changed.Task;
            ProcessSignals?.Invoke();
            if (completion.IsCompleted)
                break;
            await Task.WhenAny(completion, changedTask).WaitAsync(cancellation);
        }
        cancellation.ThrowIfCancellationRequested();
        await completion;
    }

    public int Count
    {
        get
        {
            ProcessSignals?.Invoke();
            lock (gate)
                return admittedCount;
        }
    }

    public void Send(Term message, bool priority = false)
    {
        SendTraced(
            message,
            null,
            false,
            priority
        );
    }

    internal void SendTraced(
        Term message,
        SequenceTraceToken? token,
        bool preserve = false,
        bool priority = false
    )
    {
        lock (gate)
        {
            if (priority)
                lastPriority = lastPriority is null ? messages.AddFirst(new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve
                )) : messages.AddAfter(lastPriority, new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve
                ));
            else
                messages.AddLast(new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve
                ));
            admittedCount++;
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
    }

    internal void SendPending(
        Term message,
        Func<bool?> admission,
        SequenceTraceToken? token = null,
        bool preserve = false
    )
    {
        lock (gate)
        {
            messages.AddLast(new Entry(
                message,
                admission,
                true,
                token,
                preserve
            ));
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
                    pending--;
                    node.Value = node.Value with { Admission = null, Control = true };
                    bool? priority = admission();
                    if (priority is null)
                        RemoveNode(node);
                    else
                    {
                        node.Value = node.Value with { Control = false };
                        admittedCount++;
                        if (priority.Value)
                        {
                            messages.Remove(node);
                            if (lastPriority is null)
                                messages.AddFirst(node);
                            else
                                messages.AddAfter(lastPriority, node);
                            lastPriority = node;
                        }
                    }
                }
                node = next is not null && next.List == messages ? next : null;
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

    public bool RemoveFirst(Func<Term, bool> predicate)
    {
        ProcessSignals?.Invoke();
        lock (gate)
        {
            for (var node = messages.First; node is not null; node = node.Next)
                if (!node.Value.Control && predicate(node.Value.Value))
                {
                    RemoveNode(node);

                    return true;
                }

            return false;
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
                if (!n.Value.Control && predicate(n.Value.Value))
                {
                    RemoveNode(n);
                    count++;
                }
                n = next;
            }

            return count;
        }
    }

    internal async Task WaitForMessage(CancellationToken cancellation)
    {
        try
        {
            await WaitForMessageCore(cancellation);
        }
        finally
        {
            WaitingStateChanged?.Invoke(false);
        }
    }

    private async Task WaitForMessageCore(CancellationToken cancellation)
    {
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            ProcessSignals?.Invoke();
            cancellation.ThrowIfCancellationRequested();
            if (WaitForExecution is not null)
                await WaitForExecution();
            Task signal;
            bool retry;
            lock (gate)
            {
                retry = pending != 0;
                if (!retry && messages.Count != 0)
                    return;
                signal = changed.Task;
            }
            if (retry || ExecutionBlocked?.Invoke() == true)
                continue;
            WaitingStateChanged?.Invoke(true);
            await signal.WaitAsync(cancellation);
        }
    }

    public async ValueTask<T?> ReceiveAsync<T>(Func<Term, T?> select, TimeSpan? timeout, CancellationToken cancellation = default) where T : class
    {
        try
        {
            return await ReceiveCore(select, timeout, cancellation);
        }
        finally
        {
            WaitingStateChanged?.Invoke(false);
        }
    }

    private async ValueTask<T?> ReceiveCore<T>(Func<Term, T?> select, TimeSpan? timeout, CancellationToken cancellation) where T : class
    {
        if (timeout < TimeSpan.Zero)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        long start = Stopwatch.GetTimestamp();
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            ProcessSignals?.Invoke();
            cancellation.ThrowIfCancellationRequested();
            if (WaitForExecution is not null)
                start += await WaitForExecution();
            Task signal;
            bool retry;
            T? selected = null;
            Entry received = default;
            lock (gate)
            {
                retry = pending != 0;
                for (var node = retry ? null : messages.First; node != null; node = node.Next)
                {
                    var match = select(node.Value.Value);
                    if (match is not null)
                    {
                        selected = match;
                        received = node.Value;
                        RemoveNode(node);
                        break;
                    }
                }
                signal = changed.Task;
            }
            if (selected is not null)
            {
                MessageReceived?.Invoke();
                TraceReceived?.Invoke(received.Value, received.TraceToken, received.PreserveTrace);

                return selected;
            }
            if (retry || ExecutionBlocked?.Invoke() == true)
                continue;
            TimeSpan? remaining = timeout - Stopwatch.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero)
            {
                ReceiveTimedOut?.Invoke();

                return null;
            }
            try
            {
                if (remaining is { } finite)
                {
                    WaitingStateChanged?.Invoke(true);
                    await signal.WaitAsync(
                        finite > TimeSpan.FromMilliseconds(MailboxWaitParameters.MaximumSegmentMilliseconds) ? TimeSpan.FromMilliseconds(MailboxWaitParameters.MaximumSegmentMilliseconds) : finite,
                        cancellation
                    );
                }
                else
                {
                    WaitingStateChanged?.Invoke(true);
                    await signal.WaitAsync(cancellation);
                }
            }
            catch (TimeoutException)
            {
                continue;
            }
        }
    }

    private void RemoveNode(LinkedListNode<Entry> node)
    {
        if (!node.Value.Control)
            admittedCount--;
        if (ReferenceEquals(node, lastPriority))
            lastPriority = node.Previous;
        messages.Remove(node);
    }

    internal IReadOnlyList<Term> Snapshot()
    {
        lock (gate)
            return messages.Where(entry => !entry.Control).Select(entry => entry.Value).ToArray();
    }
}
