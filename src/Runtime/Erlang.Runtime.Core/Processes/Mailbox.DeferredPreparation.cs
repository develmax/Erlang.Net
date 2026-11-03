namespace Erlang;

public sealed partial class Mailbox
{
    internal bool HasDeferredPending => !Volatile.Read(ref closed593) && Volatile.Read(ref deferredPending) != 0;

    internal bool DeferredGateHeld => Monitor.IsEntered(gate);

    internal void SendDeferred(
        Func<DeferredMailboxAdmission?> prepare,
        Action release,
        Term? from,
        Term? traceToken
    )
    {
        if (!TrySendDeferred606(
            prepare,
            release,
            from,
            traceToken
        ))
            release(); // Rejected capture cleanup is outside mailbox M.
    }

    // Runtime callers holding R inspect this result and release outside R/M.
    internal bool TrySendDeferred606(
        Func<DeferredMailboxAdmission?> prepare,
        Action release,
        Term? from,
        Term? traceToken
    )
    {
        if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true)
            return false;
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true)
                return false;
            var pendingNode = new LinkedListNode<Entry>(new Entry(
                Nil.Value,
                null,
                Control: true,
                From: from,
                DistributionTraceToken: traceToken,
                DeferredPreparation: prepare,
                DeferredRelease: release,
                Deferred: true
            ));
            AddPendingSignal428(pendingNode);
            messages.AddLast(pendingNode);
            deferredPending++;
            pending++;
            mutationRevision++;
            precedenceRevision++;
            var previous = changed;
            changed = NewSignal();
            previous.TrySetResult();
        }
        PendingSignalAdded428?.Invoke(null);

        return true;
    }

    internal MailboxDeferredPreparation? ClaimDeferred() => ClaimDeferredCore(false);

    internal MailboxDeferredPreparation? ClaimDeferredForBacking() => ClaimDeferredCore(true);

    private MailboxDeferredPreparation? ClaimDeferredCore(bool outerRelease)
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || outerRelease && policyMoveReady) return null;
            for (var node = messages.First; node is not null; node = node.Next)
            {
                if (node.Value.DeferredPreparation is not { } prepare)
                    continue;
                var captured = node;
                var claim = new MailboxDeferredPreparation(
                    prepare,
                    result => CompleteDeferred(captured, result),
                    outerRelease ? node.Value.DeferredRelease : null,
                    outerRelease && AllowsDeferredBacking(node.Value.DistributionTraceToken)
                );
                // Allocate the claim BEFORE removing either callback. Actual
                // runtime drainers own release outside R/M; legacy reflection-
                // visible claims preserve their existing Complete contract.
                node.Value = node.Value with
                {
                    DeferredPreparation = null,
                    DeferredRelease = outerRelease ? null : node.Value.DeferredRelease
                };

                return claim;
            }

            return null;
        }
    }

    private void CompleteDeferred(LinkedListNode<Entry> node, DeferredMailboxAdmission? result)
    {
        Term? admitted = null, from = null;
        System.Numerics.BigInteger copies = System.Numerics.BigInteger.Zero;
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || !ReferenceEquals(node.List, messages))
                return;
            RemovePendingSignal428(node);
            deferredPending--;
            pending--;
            mutationRevision++;
            precedenceRevision++;
            Action? release = node.Value.DeferredRelease;
            // Claim before callback: reentrant exit may capture this same Entry.
            node.Value = node.Value with { Deferred = false, DeferredRelease = null };
            release?.Invoke();
            if (Volatile.Read(ref closed593)) return;
            if (result is null || result.Copies <= 0)
            {
                RemoveNode(node);

                return;
            }
            MailboxEntryCustody593? custody641 = PrepareCustody593(result.BackingPublication ?? result.Payload641?.Publication641);
            node.Value = node.Value with { Value = result.Message, Control = false, Custody593 = custody641 };
            custody641?.Publish();
            admittedCount++;
            ObserveQueueLength419(admittedCount);
            if (Volatile.Read(ref closed593)) return;
            if (result.Priority)
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
            if (node.Value.TraceAdmission)
            {
                admitted = result.Message;
                from = node.Value.From;
            }
            copies = result.Copies;
            var tail = node;
            for (System.Numerics.BigInteger remaining = copies - 1; remaining > 0; remaining--)
            {
                // Node allocation precedes the extra interest. Linking the
                // preallocated node cannot allocate after custody is acquired.
                var copied593 = new LinkedListNode<Entry>(node.Value with { Custody593 = null });
                MailboxEntryCustody593? interest593 = node.Value.Custody593?.CloneForSameMailbox();
                try
                {
                    copied593.Value = node.Value with { Custody593 = interest593 };
                    messages.AddAfter(tail, copied593);
                    tail = copied593;
                }
                catch
                {
                    interest593?.Dispose();
                    throw;
                }
                admittedCount++;
                mutationRevision++;
                precedenceRevision++;
                ObserveQueueLength419(admittedCount);
                // A later duplicate's queue observation can also close this
                // owner. Do not reuse its tail or publish a subsequent copy.
                if (Volatile.Read(ref closed593)) return;
                if (result.Priority) lastPriority = tail;
            }
        }
        if (admitted is not null)
            for (System.Numerics.BigInteger remaining = copies; remaining > 0; remaining--)
                if (result?.BackingPublication is { } preparedPublication && MessageAdmittedSnapshot593 is { } tracedPrepared)
                    tracedPrepared(admitted, from, preparedPublication);
                else if (result?.Payload641 is { } source641 && MessageAdmittedSnapshot593 is { } traced641)
                    traced641(admitted, from, source641.Publication641);
                else
                    MessageAdmitted?.Invoke(admitted, from);
    }

    private static bool AllowsDeferredBacking(Term? rawTrace)
        => rawTrace is null or Nil
            || rawTrace is Atom preserved && preserved.Name == DistributionSequenceTraceNames.PreserveMarker;

}
