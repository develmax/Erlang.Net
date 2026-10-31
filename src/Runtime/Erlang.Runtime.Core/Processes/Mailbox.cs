using System.Diagnostics;

namespace Erlang;

public sealed partial class Mailbox
{
    private readonly object gate = new();
    internal Pid? Owner { get; init; }
    internal bool CopySelfMessages { get; init; }
    private readonly LinkedList<Entry> messages = new();
    private LinkedListNode<Entry>? lastPriority;
    private int deferredPending;
    private int pending;
    private int admittedCount;
    private long precedenceRevision;
    private long mutationRevision;
    private long pulseRevision;
    private TaskCompletionSource changed = NewSignal();
    internal Func<bool>? HaltRequested { get; init; }
    internal Action? ProcessSignals { get; init; }
    internal Func<ValueTask>? ProcessStoragePolicies { get; init; }
    internal Func<ValueTask<long>>? WaitForExecution { get; init; }
    internal Func<long>? SuspensionTime { get; init; }
    internal Func<bool>? ExecutionBlocked { get; init; }
    internal Action? MessageReceived { get; init; }
    internal Action<Term, Term?>? MessageAdmitted { get; init; }
    internal Action<Term, Term?, MailboxPublicationScope593>? MessageAdmittedSnapshot593 { get; init; }
    internal Action<int>? QueueLengthChanged { get; init; }
    internal Action<Term, SequenceTraceToken?, bool>? TraceReceived { get; init; }
    internal Action? ReceiveTimedOut { get; init; }
    internal Action<bool>? WaitingStateChanged { get; init; }

    internal readonly record struct Entry(
        Term Value,
        Func<bool?>? Admission,
        bool Control = false,
        SequenceTraceToken? TraceToken = null,
        bool PreserveTrace = false,
        Term? From = null,
        bool TraceAdmission = true,
        Term? DistributionTraceToken = null,
        Func<DeferredMailboxAdmission?>? DeferredPreparation = null,
        Action? DeferredRelease = null,
        bool Deferred = false,
        BeamReceiveMarker? ReceiveMarker = null,
        MailboxEntryCustody593? Custody593 = null,
        Exception? RetirementFailure593 = null,
        bool PendingBacking = false
    );

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void StopForHalt() => CloseForExit593();

    private void ThrowIfHalted()
    {
        if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true)
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
            await ProcessSignalsAndStorageAsync();
            if (completion.IsCompleted)
                break;
            await Task.WhenAny(completion, changedTask).WaitAsync(cancellation);
        }
        ThrowIfHalted();
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

    internal Action<Term>? IncomingTerm { get; init; }
    internal Func<Term, bool, bool>? TrySendBacked { get; init; }
    internal bool IsClosedForBacking => Volatile.Read(ref closed593);

    public void Send(Term message, bool priority = false)
    {
        // Only the owner-bound runtime hook can prepare actual backing. The
        // existing path remains for standalone/locked/system callers.
        if (!Monitor.IsEntered(gate) && TrySendBacked?.Invoke(message, priority) == true)
            return;
        SendTraced(
            message,
            null,
            false,
            priority
        );
    }

    internal void SendTracedSnapshot593(
        Term message,
        SequenceTraceToken? token,
        bool preserve = false,
        bool priority = false,
        Term? from = null,
        bool traceAdmission = true,
        Term? distributionTraceToken = null,
        MailboxPublicationScope593? publication593 = null
    )
    {
        IncomingTerm?.Invoke(message);
        if (from is not null)
            IncomingTerm?.Invoke(from);
        MailboxEntryCustody593? custody593 = PrepareCustody593(publication593);
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true)
                return;
            if (CopySelfMessages || priority || from is null || !from.Equals(Owner) || token is not null)
                BinarySharingCursor.Pin(message);
            if (priority)
                BeforeBeamPriorityInsert(message);
            if (priority)
                lastPriority = lastPriority is null ? messages.AddFirst(new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve,
                    from,
                    traceAdmission,
                    distributionTraceToken,
                    Custody593: custody593
                )) : messages.AddAfter(
                    lastPriority,
                    new Entry(
                        message,
                        null,
                        false,
                        token,
                        preserve,
                        from,
                        traceAdmission,
                        distributionTraceToken,
                        Custody593: custody593
                    )
                );
            else
                messages.AddLast(new Entry(
                    message,
                    null,
                    false,
                    token,
                    preserve,
                    from,
                    traceAdmission,
                    distributionTraceToken,
                    Custody593: custody593
                ));
            custody593?.Publish();
            mutationRevision++;
            if (priority)
            {
                receiveMarkerPriorityRevision++;
                precedenceRevision++;
            }
            admittedCount++;
            ObserveQueueLength419(admittedCount);
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
        if (traceAdmission)
        {
            if (publication593 is not null && MessageAdmittedSnapshot593 is { } traced593)
                traced593(message, from, publication593);
            else
                MessageAdmitted?.Invoke(message, from);
        }
    }

    internal void SendPendingSnapshot593(
        Term message,
        Func<bool?> admission,
        SequenceTraceToken? token = null,
        bool preserve = false,
        Term? from = null,
        bool traceAdmission = true,
        Term? distributionTraceToken = null,
        MailboxPublicationScope593? publication593 = null,
        bool prepareBacking = false
    )
        => SendPendingCore593(
            message,
            admission,
            token,
            preserve,
            from,
            traceAdmission,
            distributionTraceToken,
            null,
            publication593,
            prepareBacking
        );

    internal void SendPendingPriority428(
        Term message,
        Func<bool?> admission,
        ProcessSchedulingPriority priority,
        Term? from
    )
        => SendPendingCore593(
            message,
            admission,
            null,
            false,
            from,
            false,
            null,
            priority
        );

    private void SendPendingCore593(
        Term message,
        Func<bool?> admission,
        SequenceTraceToken? token = null,
        bool preserve = false,
        Term? from = null,
        bool traceAdmission = true,
        Term? distributionTraceToken = null
        ,
        ProcessSchedulingPriority? notificationPriority428 = null,
        MailboxPublicationScope593? publication593 = null,
        bool prepareBacking = false
    )
    {
        IncomingTerm?.Invoke(message);
        if (from is not null)
            IncomingTerm?.Invoke(from);
        MailboxEntryCustody593? custody593 = PrepareCustody593(publication593);
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true)
                return;
            BinarySharingCursor.Pin(message);
            var pendingNode = new LinkedListNode<Entry>(new Entry(
                message,
                admission,
                true,
                token,
                preserve,
                from,
                traceAdmission,
                distributionTraceToken,
                Custody593: custody593,
                PendingBacking: prepareBacking
            ));
            AddPendingSignal428(pendingNode);
            messages.AddLast(pendingNode);
            custody593?.Publish();
            pending++;
            mutationRevision++;
            precedenceRevision++;
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
        PendingSignalAdded428?.Invoke(notificationPriority428);
    }

    internal void AdmitPending()
    {
        var admitted = new List<(Term Message, Term? From)>();
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true || pending == 0 || policyMoveReady)
                return;
            for (var node = messages.First; node is not null;)
            {
                var next = node.Next;
                if (node.Value.Deferred || node.Value.PendingBacking)
                    break;
                if (node.Value.Admission is { } admission)
                {
                    RemovePendingSignal428(node);
                    pending--;
                    mutationRevision++;
                    precedenceRevision++;
                    node.Value = node.Value with { Admission = null, Control = true };
                    bool? priority = admission();
                    // Admission may terminate this owner reentrantly. Its
                    // atomic closed latch invalidates this cached node outcome.
                    if (Volatile.Read(ref closed593))
                        break;
                    if (priority is null)
                        RemoveNode(node);
                    else
                    {
                        node.Value = node.Value with { Control = false };
                        admittedCount++;
                        ObserveQueueLength419(admittedCount);
                        if (Volatile.Read(ref closed593))
                            break;
                        if (node.Value.TraceAdmission)
                            admitted.Add((node.Value.Value, node.Value.From));
                        if (priority.Value)
                        {
                            receiveMarkerPriorityRevision++;
                            BeforeBeamPriorityInsert(node.Value.Value);
                            BeforeBeamSaveUnlink(node);
                            messages.Remove(node);
                            if (lastPriority is null)
                                messages.AddFirst(node);
                            else
                                messages.AddAfter(lastPriority, node);
                            lastPriority = node;
                        }
                    }
                }
                // The actual MOVE marker is an ordering barrier. Later GC,
                // exit and monitor callbacks wait for real outside-lock movement.
                if (policyMoveReady) break;
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
            if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true || pending == 0)
                return;
            for (var node = messages.First; node is not null;)
            {
                var next = node.Next;
                if (node.Value.Admission is not null || node.Value.Deferred)
                {
                    Action? release = node.Value.DeferredRelease;
                    node.Value = node.Value with { DeferredRelease = null };
                    release?.Invoke();
                    if (Volatile.Read(ref closed593))
                        return;
                    RemoveNode(node);
                }
                node = next is not null && ReferenceEquals(next.List, messages) ? next : null;
            }
            pending = 0;
            deferredPending = 0;
        }
    }

    public bool RemoveFirst(Func<Term, bool> predicate) => RemoveFirstCore593(predicate, true);

    internal bool RemoveFirstControl593(Func<Term, bool> predicate) => RemoveFirstCore593(predicate, false);

    private bool RemoveFirstCore593(Func<Term, bool> predicate, bool opaque)
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
                IDisposable? operation = null;
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
                    operation = candidate.Custody593?.BorrowOperation();
                }
                Exception? primary = null;
                try
                {
                    Term input = opaque ? OpaqueCandidate593(candidate.Value) : candidate.Value;
                    if (opaque)
                    {
                        operation?.Dispose();
                        operation = null;
                    }
                    bool matched = predicate(input);
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
                catch (Exception error)
                {
                    primary = error;
                    throw;
                }
                finally
                {
                    MailboxReceiveCleanup593.Dispose(
                        null,
                        null,
                        operation,
                        primary
                    );
                }
            }
            if (!retry) return false;
            ProcessSignals?.Invoke();
        }
    }

    public int Remove(Func<Term, bool> predicate) => RemoveCore593(predicate, true);

    internal int RemoveControl593(Func<Term, bool> predicate) => RemoveCore593(predicate, false);

    private int RemoveCore593(Func<Term, bool> predicate, bool opaque)
    {
        ProcessSignals?.Invoke();
        while (true)
        {
            (LinkedListNode<Entry> Node, Entry Value)[] candidates;
            IDisposable?[] operations;
            Exception?[] cleanupErrors593;
            long revision;
            lock (gate)
            {
                candidates = CaptureAdmitted();
                operations = new IDisposable?[candidates.Length];
                cleanupErrors593 = new Exception?[candidates.Length];
                revision = mutationRevision;
                try
                {
                    for (int index = 0; index < candidates.Length; index++)
                        operations[index] = candidates[index].Value.Custody593?.BorrowOperation();
                }
                catch
                {
                    foreach (IDisposable? operation in operations) operation?.Dispose();
                    throw;
                }
            }
            Exception? primary = null;
            try
            {
                Term[]? opaqueInputs = opaque ? AtomicsHostAggregate594.Publish(transaction =>
                {
                    var inputs = new Term[candidates.Length];
                    for (int index = 0; index < candidates.Length; index++)
                        inputs[index] = OpaqueCandidate593(candidates[index].Value.Value, transaction);

                    // An opaque predicate may close the VM. No temporary guest
                    // operation owned by this batch may remain during callbacks.
                    for (int index = 0; index < operations.Length; index++)
                    {
                        IDisposable? operation = operations[index];
                        operations[index] = null;
                        try
                        {
                            operation?.Dispose();
                        }
                        catch (Exception error)
                        {
                            cleanupErrors593[index] = error;
                        }
                    }
                    if (cleanupErrors593.Any(error => error is not null))
                    {
                        var failure = new AggregateException(cleanupErrors593.OfType<Exception>());
                        Array.Clear(cleanupErrors593);
                        throw failure;
                    }

                    return inputs;
                }) : null;
                var matched = new List<LinkedListNode<Entry>>();
                for (int index = 0; index < candidates.Length; index++)
                {
                    Term input = opaqueInputs is null ? candidates[index].Value.Value : opaqueInputs[index];
                    if (predicate(input)) matched.Add(candidates[index].Node);
                }
                lock (gate)
                {
                    if (revision == mutationRevision && candidates.All(candidate => IsAdmitted(candidate.Node)))
                    {
                        int removed = 0;
                        foreach (var node in matched)
                        {
                            if (Volatile.Read(ref closed593)) break;
                            RemoveNode(node);
                            removed++;
                        }

                        return removed;
                    }
                }
            }
            catch (Exception error)
            {
                primary = error;
                throw;
            }
            finally
            {
                // Each operation is an atomic queue-interest drop; all are
                // attempted before allocating any aggregate error structure.
                for (int index = 0; index < operations.Length; index++)
                    try
                    {
                        operations[index]?.Dispose();
                    }
                    catch (Exception error)
                    {
                        cleanupErrors593[index] = error;
                    }
                if (cleanupErrors593.Any(error => error is not null))
                    throw new AggregateException(new[] { primary }.Concat(cleanupErrors593).OfType<Exception>());
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
            await ProcessSignalsAndStorageAsync();
            ThrowIfHalted();
            cancellation.ThrowIfCancellationRequested();
            if (WaitForExecution is not null)
                await WaitForExecution();
            Task signal;
            bool retry;
            lock (gate)
            {
                retry = pending != 0;
                if (!retry && admittedCount != 0)
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
            return await ReceiveCore(value => ValueTask.FromResult(select(value)), timeout, cancellation);
        }
        finally
        {
            WaitingStateChanged?.Invoke(false);
        }
    }

    internal async ValueTask<T?> ReceiveErlangAsync<T>(
        Func<Term, T?> select,
        Term timeout,
        CancellationToken cancellation = default
    ) where T : class
    {
        try
        {
            TimeSpan? initialTimeout = timeout is Integer integer && integer.Value.IsZero ? TimeSpan.Zero : null;

            return await ReceiveCore(
                value => ValueTask.FromResult(select(value)),
                initialTimeout,
                cancellation,
                () => ReceiveTimeoutValue.Parse(timeout)
            );
        }
        finally
        {
            WaitingStateChanged?.Invoke(false);
        }
    }

    internal async ValueTask<T?> ReceiveSelectingAsync<T>(
        Func<Term, ValueTask<T?>> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Term? erlangTimeout = null
    ) where T : class
    {
        try
        {
            TimeSpan? initial = erlangTimeout is Integer integer && integer.Value.IsZero ? TimeSpan.Zero : timeout;

            return await ReceiveCore(
                select,
                initial,
                cancellation,
                erlangTimeout is null ? null : () => ReceiveTimeoutValue.Parse(erlangTimeout)
            );
        }
        finally
        {
            WaitingStateChanged?.Invoke(false);
        }
    }

    private async ValueTask<T?> ReceiveMarkedCore593<T>(
        Func<Term, ValueTask<T?>> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Func<TimeSpan?>? resolveTimeout,
        ReceiveMarkerState? markerSelection,
        MailboxSelectionAdmission593? admission593
    ) where T : class
    {
        if (timeout < TimeSpan.Zero)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        long start = Stopwatch.GetTimestamp();
        bool timeoutResolved = resolveTimeout is null;
        bool firstBatch = true;
        bool previousConflict = false;
        LinkedListNode<Entry>? resumeAfter = null;
        long resumeRevision = 0;
        while (true)
        {
            ThrowIfHalted();
            cancellation.ThrowIfCancellationRequested();
            ThrowStatisticsTrapFailure();
            await ProcessSignalsAndStorageAsync();
            ThrowIfHalted();
            cancellation.ThrowIfCancellationRequested();
            if (WaitForExecution is not null)
                start += await WaitForExecution();
            lock (gate)
                ValidateReceiveMarkerPriority(markerSelection);
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
            IDisposable? receivedOperation593 = null;
            LinkedListNode<Entry>? node;
            long revision;
            long capturedPulse;
            LinkedListNode<Entry>? capturedTail;
            lock (gate)
            {
                ValidateReceiveMarkerPriority(markerSelection);
                retry = pending != 0;
                node = retry ? null : resumeAfter is not null && resumeRevision == precedenceRevision && IsAdmitted(resumeAfter)
                    ? resumeAfter.Next : ReceiveMarkerStart(markerSelection);
                revision = precedenceRevision;
                capturedPulse = pulseRevision;
                capturedTail = messages.Last;
                signal = changed.Task;
            }
            resumeAfter = null;
            while (node is not null)
            {
                Entry candidate;
                IDisposable? operation593 = null;
                lock (gate)
                {
                    ValidateReceiveMarkerPriority(markerSelection);
                    if (revision != precedenceRevision || pending != 0 || !ReferenceEquals(node.List, messages))
                    {
                        retry = true;
                        break;
                    }
                    if (!IsAdmitted(node))
                    {
                        var next = node.Next;
                        ObserveReceiveMarkerPass(node);
                        node = next;
                        continue;
                    }
                    candidate = node.Value;
                    operation593 = candidate.Custody593?.BorrowOperation();
                }
                T? match = null;
                MailboxGuestCandidate593? guest593 = null;
                Exception? primary593 = null;
                bool transferred504 = false;
                try
                {
                    long suspensionBefore = SuspensionTime?.Invoke() ?? 0;
                    guest593 = admission593?.Prepare(candidate.Value, candidate.Custody593?.Graph);
                    Term candidateValue593 = guest593?.Value ?? OpaqueCandidate593(candidate.Value);
                    if (admission593 is null)
                    {
                        // Host admission is complete; do not make a public
                        // callback's VM-close depend on its own Guest borrow.
                        operation593?.Dispose();
                        operation593 = null;
                    }
                    match = await select(candidateValue593);
                    guest593?.AttachTo(match);
                    start += (SuspensionTime?.Invoke() ?? 0) - suspensionBefore;
                    ThrowIfHalted();
                    cancellation.ThrowIfCancellationRequested();
                    lock (gate)
                    {
                        ThrowIfHalted();
                        cancellation.ThrowIfCancellationRequested();
                        ValidateReceiveMarkerPriority(markerSelection);
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
                            operation593 ??= candidate.Custody593?.BorrowOperation();
                            RemoveNode(node);
                            receivedOperation593 = operation593;
                            operation593 = null;
                            transferred504 = true;
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
                catch (Exception error)
                {
                    primary593 = error;
                    throw;
                }
                finally
                {
                    MailboxReceiveCleanup593.Dispose(
                        !transferred504 ? match as IGuestMailboxSelection593 : null,
                        guest593,
                        operation593,
                        primary593
                    );
                }
            }
            if (selected is null)
            {
                lock (gate)
                {
                    ValidateReceiveMarkerPriority(markerSelection);
                    previousConflict = revision != precedenceRevision || capturedPulse != pulseRevision || pending != 0;
                    retry |= previousConflict || !ReferenceEquals(capturedTail, messages.Last) || !ReferenceEquals(signal, changed.Task);
                    if (previousConflict)
                        resumeAfter = null;
                    signal = changed.Task;
                }
            }
            if (selected is not null)
            {
                Exception? traceFailure593 = null;
                try
                {
                    MessageReceived?.Invoke();
                    SequenceTraceToken? traceToken = received.TraceToken;
                    bool preserveTrace = received.PreserveTrace;
                    if (received.DistributionTraceToken is { } rawTrace)
                    {
                        var reception = DistributionSequenceTraceToken.Decode(rawTrace);
                        traceToken = reception.Token;
                        preserveTrace = reception.Preserve;
                    }
                    TraceReceived?.Invoke(received.Value, traceToken, preserveTrace);

                    return selected;
                }
                catch (Exception error)
                {
                    traceFailure593 = error;
                    throw;
                }
                finally
                {
                    MailboxReceiveCleanup593.Dispose(
                        traceFailure593 is not null ? selected as IGuestMailboxSelection593 : null,
                        null,
                        receivedOperation593,
                        traceFailure593
                    );
                }
            }
            if (timeout == TimeSpan.Zero && retry && !previousConflict && ExecutionBlocked?.Invoke() != true)
            {
                ReceiveTimedOut?.Invoke();

                return null;
            }
            if (retry || ExecutionBlocked?.Invoke() == true)
                continue;
            if (!timeoutResolved)
            {
                timeout = resolveTimeout!();
                timeoutResolved = true;
                start = Stopwatch.GetTimestamp();
            }
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
                    await WaitStatisticsTrapSignal(
                        signal,
                        finite > TimeSpan.FromMilliseconds(MailboxWaitParameters.MaximumSegmentMilliseconds) ? TimeSpan.FromMilliseconds(MailboxWaitParameters.MaximumSegmentMilliseconds) : finite,
                        cancellation
                    );
                }
                else
                {
                    WaitingStateChanged?.Invoke(true);
                    await WaitStatisticsTrapSignal(signal, null, cancellation);
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
        RemovePendingSignal428(node);
        BeforeBeamSaveUnlink(node);
        bool ordinary = !node.Value.Control && node.Value.Admission is null;
        mutationRevision++;
        precedenceRevision++;
        if (ordinary)
            admittedCount--;
        if (ReferenceEquals(node, lastPriority))
            lastPriority = node.Previous;
        messages.Remove(node);
        node.Value.Custody593?.Dispose();
        if (ordinary)
            ObserveQueueLength419(admittedCount);
    }

    internal IReadOnlyList<Term> Snapshot()
        => CompletePublicSnapshot601(CaptureSnapshot601());
}
