namespace Erlang;

public sealed partial class Mailbox
{
    // Actual pending control/deferred entries, in original admission order.
    // Receive marker and save-cursor pseudo entries are never inserted here.
    private readonly LinkedList<LinkedListNode<Entry>> pendingSignals428 = new();
    private readonly Dictionary<LinkedListNode<Entry>, LinkedListNode<LinkedListNode<Entry>>> signalNodes428 = new();

    internal Action<ProcessSchedulingPriority?>? PendingSignalAdded428 { get; init; }

    internal bool HasPendingSignals428 => !Volatile.Read(ref closed593) && Volatile.Read(ref pending) != 0;

    private void AddPendingSignal428(LinkedListNode<Entry> node)
    {
        var index = new LinkedListNode<LinkedListNode<Entry>>(node);
        signalNodes428.Add(node, index);
        pendingSignals428.AddLast(index);
    }

    private void RemovePendingSignal428(LinkedListNode<Entry> node)
    {
        if (signalNodes428.Remove(node, out var index)) pendingSignals428.Remove(index);
    }

    private void ClearPendingSignals428()
    {
        signalNodes428.Clear();
        pendingSignals428.Clear();
    }

    internal MailboxSignalBatch428 AdmitPendingBounded428(int budget)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(budget);
        var admitted = new List<(Term Message, Term? From)>();
        int processed = 0;
        bool deferred = false, backing = false, remaining;
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || HaltRequested?.Invoke() == true) return new(0, false, false);
            if (policyMoveReady) return new(0, false, pendingSignals428.Count != 0) { PolicyMove = true };
            var originalTail = pendingSignals428.Last;
            while (processed < budget && pendingSignals428.First is { } index)
            {
                var node = index.Value;
                if (node.Value.PendingBacking)
                {
                    backing = true;
                    break;
                }
                if (node.Value.Deferred)
                {
                    deferred = true;
                    break;
                }
                if (node.Value.Admission is not { } admission)
                    throw new InvalidOperationException(SystemTaskNames428.SignalIndex);
                bool reachedTail = ReferenceEquals(index, originalTail);
                RemovePendingSignal428(node);
                pending--;
                processed++;
                mutationRevision++;
                precedenceRevision++;
                node.Value = node.Value with { Admission = null, Control = true };
                bool? priority = admission();
                if (Volatile.Read(ref closed593)) break;
                if (priority is null) RemoveNode(node);
                else
                {
                    node.Value = node.Value with { Control = false };
                    admittedCount++;
                    ObserveQueueLength419(admittedCount);
                    if (Volatile.Read(ref closed593)) break;
                    if (node.Value.TraceAdmission) admitted.Add((node.Value.Value, node.Value.From));
                    if (priority.Value)
                    {
                        receiveMarkerPriorityRevision++;
                        BeforeBeamPriorityInsert(node.Value.Value);
                        BeforeBeamSaveUnlink(node);
                        messages.Remove(node);
                        if (lastPriority is null) messages.AddFirst(node); else messages.AddAfter(lastPriority, node);
                        lastPriority = node;
                    }
                }
                // Reentrant callbacks may remove the original tail. Conservatively
                // yield before newly appended signals instead of chasing them forever.
                if (policyMoveReady || reachedTail || originalTail?.List is null) break;
            }
            remaining = !Volatile.Read(ref closed593) && pendingSignals428.Count != 0;
        }
        foreach (var item in admitted) MessageAdmitted?.Invoke(item.Message, item.From);

        return new(processed, deferred, remaining) { PendingBacking = backing, PolicyMove = policyMoveReady };
    }

    internal MailboxDeferredPreparation? ClaimDeferredBounded428() => ClaimDeferredBoundedCore(false);

    internal MailboxDeferredPreparation? ClaimDeferredBoundedForBacking() => ClaimDeferredBoundedCore(true);

    private MailboxDeferredPreparation? ClaimDeferredBoundedCore(bool outerRelease)
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || outerRelease && policyMoveReady || pendingSignals428.First is not { } first) return null;
            var node = first.Value;
            if (!node.Value.Deferred || node.Value.DeferredPreparation is not { } prepare) return null;
            var claim = new MailboxDeferredPreparation(
                prepare,
                result => CompleteDeferred(node, result),
                outerRelease ? node.Value.DeferredRelease : null,
                outerRelease && AllowsDeferredBacking(node.Value.DistributionTraceToken)
            );
            node.Value = node.Value with
            {
                DeferredPreparation = null,
                DeferredRelease = outerRelease ? null : node.Value.DeferredRelease
            };

            return claim;
        }
    }

}
