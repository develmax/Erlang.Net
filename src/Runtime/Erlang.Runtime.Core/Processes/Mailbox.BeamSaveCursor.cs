using System.Diagnostics;

namespace Erlang;

public sealed partial class Mailbox
{
    private BeamSaveCursor? beamSave;

    private sealed class BeamSaveCursor
    {
        internal LinkedListNode<Entry>? After;
        internal LinkedListNode<Entry>? PriorityContinuation;
        internal bool InPriority;
        internal ReceiveMarkerState? Marker;
    }

    private void EnableBeamSave()
    {
        beamSave ??= new() { InPriority = lastPriority is not null };
    }

    private void SetBeamSaveMarker(ReceiveMarkerState marker)
    {
        if (beamSave is not { } save)
            return;
        ResetBeamSave();
        save.After = marker.Node;
        save.Marker = marker;
        marker.CursorUsers++;
        save.InPriority = false;
        if (marker.SeenInBeamPriority && lastPriority is not null)
        {
            save.PriorityContinuation = messages.AddAfter(marker.Node, new Entry(Nil.Value, null, Control: true));
            mutationRevision++;
            save.After = null;
            save.InPriority = true;
        }
    }

    private void ResetBeamSave()
    {
        if (beamSave is not { } save)
            return;
        save.After = null;
        save.InPriority = lastPriority is not null;
        if (save.PriorityContinuation is { } continuation)
        {
            save.PriorityContinuation = null;
            if (ReferenceEquals(continuation.List, messages))
                RemoveNode(continuation);
        }
        if (save.Marker is { } marker)
        {
            save.Marker = null;
            marker.CursorUsers--;
            ReleaseReceiveMarkerNode(marker);
        }
    }

    private void BeforeBeamSaveUnlink(LinkedListNode<Entry> node)
    {
        if (beamSave is not { } save)
            return;
        if (ReferenceEquals(save.After, node))
            save.After = node.Previous;
        if (ReferenceEquals(save.PriorityContinuation, node))
            save.PriorityContinuation = null;
    }

    private LinkedListNode<Entry>? PeekBeamSave()
    {
        BeamSaveCursor save = beamSave!;
        while (true)
        {
            if (save.InPriority && (lastPriority is null || ReferenceEquals(save.After, lastPriority)))
            {
                save.InPriority = false;
                if (save.PriorityContinuation is { } continuation)
                {
                    save.PriorityContinuation = null;
                    save.After = continuation;
                    RemoveNode(continuation);
                }
            }
            LinkedListNode<Entry>? node = save.After is null ? messages.First : save.After.Next;
            if (node is null || IsAdmitted(node))
                return node;
            save.After = node;
            ObserveReceiveMarkerPass(node);
        }
    }

    private void AdvanceBeamSave()
    {
        LinkedListNode<Entry> current = PeekBeamSave() ?? throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
        beamSave!.After = current;
    }

    private void BeforeBeamPriorityInsert(Term message)
    {
        if (beamSave is not { } save)
            return;
        if (lastPriority is null)
        {
            foreach (ReceiveMarkerState marker in receiveMarkers)
                marker.SeenInBeamPriority = false;
            if (save.Marker is { Retired: true } retired)
                retired.SeenInBeamPriority = false;
        }
        var pending = new Stack<Term>();
        var visited = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(message);
        while (pending.TryPop(out Term? term))
        {
            if (!visited.Add(term))
                continue;
            if (TryReceiveMarkerReference(term, out var reference))
                foreach (ReceiveMarkerState marker in receiveMarkers)
                    if (marker.ReferenceId == reference)
                        marker.SeenInBeamPriority = true;
            switch (term)
            {
                case Cons list:
                    pending.Push(list.Head);
                    pending.Push(list.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (Term value in tuple.Items)
                        pending.Push(value);
                    break;
                case NativeRecordTerm record:
                    foreach (Term value in record.Values)
                        pending.Push(value);
                    break;
                case MapTerm map:
                    foreach (var pair in map.Entries)
                    {
                        pending.Push(pair.Key);
                        pending.Push(pair.Value);
                    }
                    break;
                case FunctionTerm function:
                    foreach (Term value in function.Environment)
                        pending.Push(value);
                    break;
            }
        }
        if (save.InPriority || save.Marker is { SeenInBeamPriority: false })
            return;
        if (save.PriorityContinuation is { } prior)
            RemoveNode(prior);
        LinkedListNode<Entry>? current = save.After is null ? messages.First : save.After.Next;
        var entry = new Entry(Nil.Value, null, Control: true);
        save.PriorityContinuation = current is null ? messages.AddLast(entry) : messages.AddBefore(current, entry);
        mutationRevision++;
        save.After = lastPriority;
        save.InPriority = true;
    }

    internal ValueTask<T?> ReceiveBeamAdmitted593<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        CancellationToken cancellation,
        MailboxSelectionAdmission593? admission593 = null
    ) where T : class => ReceiveBeamSelectionAdmitted593(
        select,
        timeout,
        cancellation,
        rescanRemovedCandidate: false,
        admission593
    );

    internal ValueTask<T?> ReceiveBeamGuardAdmitted593<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        CancellationToken cancellation,
        MailboxSelectionAdmission593? admission593 = null
    ) where T : class => ReceiveBeamSelectionAdmitted593(
        select,
        timeout,
        cancellation,
        rescanRemovedCandidate: true,
        admission593
    );

    private async ValueTask<T?> ReceiveBeamSelectionAdmitted593<T>(
        Func<Term, ValueTask<T?>> select,
        Term timeout,
        CancellationToken cancellation,
        bool rescanRemovedCandidate = false,
        MailboxSelectionAdmission593? admission593 = null
    ) where T : class
    {
        lock (gate)
            EnableBeamSave();
        try
        {
            TimeSpan? initial = timeout is Integer integer && integer.Value.IsZero ? TimeSpan.Zero : null;

            return await ReceiveCore593(
                select,
                initial,
                cancellation,
                () => ReceiveTimeoutValue.Parse(timeout),
                rescanRemovedCandidate,
                admission593
            );
        }
        finally
        {
            WaitingStateChanged?.Invoke(false);
        }
    }

    private async ValueTask<T?> ReceiveBeamSaveCore593<T>(
        Func<Term, ValueTask<T?>> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Func<TimeSpan?>? resolveTimeout,
        bool rescanRemovedCandidate,
        MailboxSelectionAdmission593? admission593
    ) where T : class
    {
        if (timeout < TimeSpan.Zero)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        long start = Stopwatch.GetTimestamp();
        bool resolved = resolveTimeout is null;
        while (true)
        {
            ThrowIfHalted();
            cancellation.ThrowIfCancellationRequested();
            ProcessSignals?.Invoke();
            if (WaitForExecution is not null)
                start += await WaitForExecution();
            Task signal;
            Entry candidate = default;
            IDisposable? operation593 = null;
            LinkedListNode<Entry>? candidateNode = null;
            bool found;
            bool admissionPending;
            lock (gate)
            {
                admissionPending = pending != 0;
                var node = admissionPending ? null : PeekBeamSave();
                candidateNode = node;
                found = node is not null;
                if (found)
                {
                    candidate = node!.Value;
                    operation593 = candidate.Custody593?.BorrowOperation();
                }
                signal = changed.Task;
            }
            if (admissionPending || ExecutionBlocked?.Invoke() == true)
            {
                operation593?.Dispose();
                continue;
            }
            if (found)
            {
                T? result = null;
                MailboxGuestCandidate593? guest593 = null;
                IDisposable? consumedOperation593 = null;
                Exception? primary593 = null;
                bool transferred504 = false;
                try
                {
                    long suspended = SuspensionTime?.Invoke() ?? 0;
                    if (admission593 is not null)
                        await admission593.ResumeExecution();
                    guest593 = admission593?.Prepare(candidate.Value, candidate.Custody593?.Graph);
                    Term candidateValue593 = guest593?.Value ?? OpaqueCandidateWithCustody292(candidate.Value, candidate.Custody593, operation593);
                    if (admission593 is null)
                    {
                        operation593?.Dispose();
                        operation593 = null;
                    }
                    result = await select(candidateValue593);
                    guest593?.AttachTo(result);
                    start += (SuspensionTime?.Invoke() ?? 0) - suspended;
                    while (true)
                    {
                        ThrowIfHalted();
                        cancellation.ThrowIfCancellationRequested();
                        ProcessSignals?.Invoke();
                        if (WaitForExecution is not null)
                            start += await WaitForExecution();
                        Entry consumed = default;
                        bool retry;
                        bool rescan = false;
                        lock (gate)
                        {
                            ThrowIfHalted();
                            cancellation.ThrowIfCancellationRequested();
                            retry = pending != 0 || ExecutionBlocked?.Invoke() == true;
                            // Priority insertion keeps the guard result for its prior candidate
                            // and commits through the current save pointer, as OTP i_length does.
                            // Host-side removal instead invalidates candidate ownership.
                            if (!retry && rescanRemovedCandidate && !ReferenceEquals(candidateNode?.List, messages))
                                rescan = true;
                            if (!retry && !rescan)
                            {
                                if (result is null)
                                    AdvanceBeamSave();
                                else
                                {
                                    var current = PeekBeamSave() ?? throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
                                    consumed = current.Value;
                                    consumedOperation593 = consumed.Custody593?.BorrowOperation();
                                    RemoveNode(current);
                                    ResetBeamSave();
                                }
                            }
                        }
                        if (retry)
                            continue;
                        if (rescan)
                            break;
                        if (result is null)
                            break;
                        MessageReceived?.Invoke();
                        SequenceTraceToken? traceToken = consumed.TraceToken;
                        bool preserve = consumed.PreserveTrace;
                        if (consumed.DistributionTraceToken is { } raw)
                        {
                            var reception = DistributionSequenceTraceToken.Decode(raw);
                            traceToken = reception.Token;
                            preserve = reception.Preserve;
                        }
                        TraceReceived?.Invoke(consumed.Value, traceToken, preserve);

                        transferred504 = true;

                        return result;
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
                        !transferred504 ? result as IGuestMailboxSelection593 : null,
                        guest593,
                        operation593,
                        primary593,
                        consumedOperation593
                    );
                }
                continue;
            }
            if (!resolved)
            {
                try
                {
                    timeout = resolveTimeout!();
                }
                catch
                {
                    lock (gate)
                        ResetBeamSave();
                    throw;
                }
                resolved = true;
                start = Stopwatch.GetTimestamp();
            }
            TimeSpan? remaining = timeout - Stopwatch.GetElapsedTime(start);
            if (remaining <= TimeSpan.Zero)
            {
                lock (gate)
                    ResetBeamSave();
                ReceiveTimedOut?.Invoke();

                return null;
            }
            try
            {
                WaitingStateChanged?.Invoke(true);
                if (remaining is { } finite)
                    await signal.WaitAsync(
                        finite > TimeSpan.FromMilliseconds(MailboxWaitParameters.MaximumSegmentMilliseconds)
                            ? TimeSpan.FromMilliseconds(MailboxWaitParameters.MaximumSegmentMilliseconds) : finite,
                        cancellation
                    );
                else
                    await signal.WaitAsync(cancellation);
            }
            catch (TimeoutException)
            {
                // Resume at the wait instruction, preserving the existing timer.
            }
            finally
            {
                WaitingStateChanged?.Invoke(false);
            }
        }
    }
}
