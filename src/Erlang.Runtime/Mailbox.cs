using System.Diagnostics;

namespace Erlang;

public sealed class Mailbox
{
    private readonly object gate = new();
    private readonly LinkedList<Entry> messages = new();
    private LinkedListNode<Entry>? lastPriority;
    private int pending;
    private int admittedCount;
    private long precedenceRevision;
    private long mutationRevision;
    private long pulseRevision;
    private TaskCompletionSource changed = NewSignal();
    internal Func<bool>? HaltRequested { get; init; }
    internal Action? ProcessSignals { get; init; }
    internal Func<ValueTask<long>>? WaitForExecution { get; init; }
    internal Func<bool>? ExecutionBlocked { get; init; }
    internal Action? MessageReceived { get; init; }
    internal Action<Term, Term?>? MessageAdmitted { get; init; }
    internal Action<int>? QueueLengthChanged { get; init; }
    internal Action<Term, SequenceTraceToken?, bool>? TraceReceived { get; init; }
    internal Action? ReceiveTimedOut { get; init; }
    internal Action<bool>? WaitingStateChanged { get; init; }

    private readonly record struct Entry(
        Term Value,
        Func<bool?>? Admission,
        bool Control = false,
        SequenceTraceToken? TraceToken = null,
        bool PreserveTrace = false,
        Term? From = null,
        bool TraceAdmission = true
    );

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void StopForHalt()
    {
        lock (gate)
        {
            mutationRevision++;
            precedenceRevision++;
            messages.Clear();
            pending = 0;
            admittedCount = 0;
            lastPriority = null;
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
    }

    private void ThrowIfHalted()
    {
        if (HaltRequested?.Invoke() == true)
            throw new OperationCanceledException();
    }

    internal void Pulse()
    {
        lock (gate)
        {
            pulseRevision++;
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
    }

    internal async ValueTask WaitForControlCompletion(Task completion, CancellationToken cancellation)
    {
        while (!completion.IsCompleted)
        {
            ThrowIfHalted();
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
        bool priority = false,
        Term? from = null,
        bool traceAdmission = true
    )
    {
        lock (gate)
        {
            if (HaltRequested?.Invoke() == true)
                return;
            if (priority)
                lastPriority = lastPriority is null ? messages.AddFirst(new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve,
                    from,
                    traceAdmission
                )) : messages.AddAfter(lastPriority, new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve,
                    from,
                    traceAdmission
                ));
            else
                messages.AddLast(new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve,
                    from,
                    traceAdmission
                ));
            mutationRevision++;
            if (priority)
                precedenceRevision++;
            admittedCount++;
            QueueLengthChanged?.Invoke(admittedCount);
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
        if (traceAdmission)
            MessageAdmitted?.Invoke(message, from);
    }

    internal void SendPending(
        Term message,
        Func<bool?> admission,
        SequenceTraceToken? token = null,
        bool preserve = false,
        Term? from = null,
        bool traceAdmission = true
    )
    {
        lock (gate)
        {
            if (HaltRequested?.Invoke() == true)
                return;
            messages.AddLast(new Entry(
                message,
                admission,
                true,
                token,
                preserve,
                from,
                traceAdmission
            ));
            pending++;
            mutationRevision++;
            precedenceRevision++;
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
    }

    internal void AdmitPending()
    {
        var admitted = new List<(Term Message, Term? From)>();
        lock (gate)
        {
            if (HaltRequested?.Invoke() == true || pending == 0)
                return;
            for (var node = messages.First; node is not null;)
            {
                var next = node.Next;
                if (node.Value.Admission is { } admission)
                {
                    pending--;
                    mutationRevision++;
                    precedenceRevision++;
                    node.Value = node.Value with { Admission = null, Control = true };
                    bool? priority = admission();
                    if (priority is null)
                        RemoveNode(node);
                    else
                    {
                        node.Value = node.Value with { Control = false };
                        admittedCount++;
                        QueueLengthChanged?.Invoke(admittedCount);
                        if (node.Value.TraceAdmission)
                            admitted.Add((node.Value.Value, node.Value.From));
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
        foreach (var admission in admitted)
            MessageAdmitted?.Invoke(admission.Message, admission.From);
    }

    internal void DiscardPending()
    {
        lock (gate)
        {
            if (HaltRequested?.Invoke() == true || pending == 0)
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
        while (true)
        {
            LinkedListNode<Entry>? node;
            long revision;
            lock (gate)
            {
                node = messages.First;
                revision = precedenceRevision;
            }
            bool retry = false;
            while (node is not null)
            {
                Entry candidate;
                lock (gate)
                {
                    if (revision != precedenceRevision || !ReferenceEquals(node.List, messages))
                    {
                        retry = true;
                        break;
                    }
                    if (!IsAdmitted(node))
                    {
                        node = node.Next;
                        continue;
                    }
                    candidate = node.Value;
                }
                bool matched = predicate(candidate.Value);
                lock (gate)
                {
                    if (revision != precedenceRevision || !IsAdmitted(node))
                    {
                        retry = true;
                        break;
                    }
                    if (matched)
                    {
                        RemoveNode(node);

                        return true;
                    }
                    node = node.Next;
                }
            }
            if (!retry)
                return false;
            ProcessSignals?.Invoke();
        }
    }

    public int Remove(Func<Term, bool> predicate)
    {
        ProcessSignals?.Invoke();
        while (true)
        {
            (LinkedListNode<Entry> Node, Entry Value)[] candidates;
            long revision;
            lock (gate)
            {
                candidates = CaptureAdmitted();
                revision = mutationRevision;
            }
            var matched = new List<LinkedListNode<Entry>>();
            foreach (var candidate in candidates)
            {
                if (predicate(candidate.Value.Value))
                    matched.Add(candidate.Node);
            }
            lock (gate)
            {
                if (revision == mutationRevision && candidates.All(candidate => IsAdmitted(candidate.Node)))
                {
                    foreach (var node in matched)
                        RemoveNode(node);

                    return matched.Count;
                }
            }
            ProcessSignals?.Invoke();
        }
    }

    private (LinkedListNode<Entry> Node, Entry Value)[] CaptureAdmitted()
    {
        var captured = new List<(LinkedListNode<Entry>, Entry)>();
        for (var node = messages.First; node is not null; node = node.Next)
        {
            if (IsAdmitted(node))
                captured.Add((node, node.Value));
        }

        return captured.ToArray();
    }

    private bool IsAdmitted(LinkedListNode<Entry> node) =>
        ReferenceEquals(node.List, messages) && !node.Value.Control && node.Value.Admission is null;

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
            ThrowIfHalted();
            cancellation.ThrowIfCancellationRequested();
            ProcessSignals?.Invoke();
            ThrowIfHalted();
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
        bool firstBatch = true;
        bool previousConflict = false;
        LinkedListNode<Entry>? resumeAfter = null;
        long resumeRevision = 0;
        while (true)
        {
            ThrowIfHalted();
            cancellation.ThrowIfCancellationRequested();
            ProcessSignals?.Invoke();
            ThrowIfHalted();
            cancellation.ThrowIfCancellationRequested();
            if (WaitForExecution is not null)
                start += await WaitForExecution();
            if (!firstBatch && timeout is { } finiteTimeout && finiteTimeout > TimeSpan.Zero && Stopwatch.GetElapsedTime(start) >= finiteTimeout)
            {
                ReceiveTimedOut?.Invoke();

                return null;
            }
            firstBatch = false;
            previousConflict = false;
            Task signal;
            bool retry;
            T? selected = null;
            Entry received = default;
            LinkedListNode<Entry>? node;
            long revision;
            long capturedPulse;
            LinkedListNode<Entry>? capturedTail;
            lock (gate)
            {
                retry = pending != 0;
                node = retry ? null : resumeAfter is not null && resumeRevision == precedenceRevision && IsAdmitted(resumeAfter)
                    ? resumeAfter.Next : messages.First;
                revision = precedenceRevision;
                capturedPulse = pulseRevision;
                capturedTail = messages.Last;
                signal = changed.Task;
            }
            resumeAfter = null;
            while (node is not null)
            {
                Entry candidate;
                lock (gate)
                {
                    if (revision != precedenceRevision || pending != 0 || !ReferenceEquals(node.List, messages))
                    {
                        retry = true;
                        break;
                    }
                    if (!IsAdmitted(node))
                    {
                        node = node.Next;
                        continue;
                    }
                    candidate = node.Value;
                }
                T? match = select(candidate.Value);
                ThrowIfHalted();
                cancellation.ThrowIfCancellationRequested();
                lock (gate)
                {
                    ThrowIfHalted();
                    cancellation.ThrowIfCancellationRequested();
                    if (revision != precedenceRevision || pending != 0 || !IsAdmitted(node)
                        || ExecutionBlocked?.Invoke() == true)
                    {
                        retry = true;
                        break;
                    }
                    if (match is not null)
                    {
                        selected = match;
                        received = candidate;
                        RemoveNode(node);
                        break;
                    }
                    if (ReferenceEquals(node, capturedTail))
                    {
                        resumeAfter = node;
                        resumeRevision = revision;
                        node = null;
                    }
                    else
                    {
                        node = node.Next;
                    }
                }
            }
            if (selected is null)
            {
                lock (gate)
                {
                    previousConflict = revision != precedenceRevision || capturedPulse != pulseRevision || pending != 0;
                    retry |= previousConflict || !ReferenceEquals(capturedTail, messages.Last) || !ReferenceEquals(signal, changed.Task);
                    if (previousConflict)
                        resumeAfter = null;
                    signal = changed.Task;
                }
            }
            if (selected is not null)
            {
                MessageReceived?.Invoke();
                TraceReceived?.Invoke(received.Value, received.TraceToken, received.PreserveTrace);

                return selected;
            }
            if (timeout == TimeSpan.Zero && retry && !previousConflict && ExecutionBlocked?.Invoke() != true)
            {
                ReceiveTimedOut?.Invoke();

                return null;
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
        bool ordinary = !node.Value.Control && node.Value.Admission is null;
        mutationRevision++;
        precedenceRevision++;
        if (ordinary)
            admittedCount--;
        if (ReferenceEquals(node, lastPriority))
            lastPriority = node.Previous;
        messages.Remove(node);
        if (ordinary)
            QueueLengthChanged?.Invoke(admittedCount);
    }

    internal IReadOnlyList<Term> Snapshot()
    {
        lock (gate)
            return messages.Where(entry => !entry.Control).Select(entry => entry.Value).ToArray();
    }
}
