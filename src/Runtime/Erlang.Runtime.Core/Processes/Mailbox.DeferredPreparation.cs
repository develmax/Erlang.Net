namespace Erlang;

public sealed partial class Mailbox
{
    internal bool HasDeferredPending => Volatile.Read(ref deferredPending) != 0;

    internal bool DeferredGateHeld => Monitor.IsEntered(gate);

    internal void SendDeferred(
        Func<DeferredMailboxAdmission?> prepare,
        Action release,
        Term? from,
        Term? traceToken
    )
    {
        lock (gate)
        {
            if (HaltRequested?.Invoke() == true)
            {
                release();

                return;
            }
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
    }

    internal MailboxDeferredPreparation? ClaimDeferred()
    {
        lock (gate)
        {
            for (var node = messages.First; node is not null; node = node.Next)
            {
                if (node.Value.DeferredPreparation is not { } prepare)
                    continue;
                node.Value = node.Value with { DeferredPreparation = null };
                var captured = node;

                return new(prepare, result => CompleteDeferred(captured, result));
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
            if (!ReferenceEquals(node.List, messages))
                return;
            RemovePendingSignal428(node);
            deferredPending--;
            pending--;
            mutationRevision++;
            precedenceRevision++;
            node.Value.DeferredRelease?.Invoke();
            node.Value = node.Value with { Deferred = false, DeferredRelease = null };
            if (result is null || result.Copies <= 0)
            {
                RemoveNode(node);

                return;
            }
            node.Value = node.Value with { Value = result.Message, Control = false };
            admittedCount++;
            ObserveQueueLength419(admittedCount);
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
                tail = messages.AddAfter(tail, node.Value);
                admittedCount++;
                mutationRevision++;
                precedenceRevision++;
                ObserveQueueLength419(admittedCount);
                if (result.Priority) lastPriority = tail;
            }
        }
        if (admitted is not null)
            for (System.Numerics.BigInteger remaining = copies; remaining > 0; remaining--)
                MessageAdmitted?.Invoke(admitted, from);
    }
}
