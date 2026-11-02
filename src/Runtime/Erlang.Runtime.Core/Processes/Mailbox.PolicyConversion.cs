namespace Erlang;

public sealed partial class Mailbox
{
    private async ValueTask ProcessSignalsAndStorageAsync()
    {
        ProcessSignals?.Invoke();
        if (ProcessStoragePolicies is { } policy) await policy();
    }

    private readonly MailboxPolicyScan policyMoveScan = new();
    internal MailboxPolicyScan PolicyMoveScan => policyMoveScan;
    private int messageQueuePlacementMode;
    private volatile bool policyMovePending;
    private volatile bool policyMoveReady;
    private bool policyMoveDraining;

    internal ProcessMessageQueueDataMode411 MessageQueuePlacementMode
        => (ProcessMessageQueueDataMode411)Volatile.Read(ref messageQueuePlacementMode);
    internal bool HasMessageQueuePolicyWork => policyMovePending;

    // Runtime R is held by the caller, so sender final-publication revision
    // revalidation is a real fence against every admitted preparation.
    internal ProcessMessageQueueDataMode411 RequestMessageQueuePolicy(ProcessMessageQueueDataMode411 mode)
    {
        bool notify = false;
        ProcessMessageQueueDataMode411 previous;
        lock (gate)
        {
            previous = MessageQueueData411;
            if (previous == mode) return previous;
            if (Volatile.Read(ref closed593)) throw new OperationCanceledException();
            if (mode == ProcessMessageQueueDataMode411.OffHeap && !policyMovePending)
            {
                // All fallible marker/index/signal allocation precedes state
                // changes. The real marker lives in the ordinary signal order.
                var marker = new LinkedListNode<Entry>(new Entry(
                    Nil.Value,
                    AdmitPolicyMoveMarker,
                    Control: true,
                    TraceAdmission: false
                ));
                var index = new LinkedListNode<LinkedListNode<Entry>>(marker);
                signalNodes428.EnsureCapacity(checked(signalNodes428.Count + 1));
                TaskCompletionSource nextSignal = NewSignal();
                signalNodes428.Add(marker, index);
                pendingSignals428.AddLast(index);
                messages.AddLast(marker);
                pending++;
                mutationRevision++;
                precedenceRevision++;
                policyMovePending = true;
                policyMoveReady = false;
                var oldSignal = changed;
                changed = nextSignal;
                oldSignal.TrySetResult();
                notify = true;
            }
            Volatile.Write(ref messageQueueData411, (int)mode);
            // OFF stays effective across requested off->on->off reversals until
            // the actual receiver marker and movement complete.
            if (mode == ProcessMessageQueueDataMode411.OffHeap || !policyMovePending)
                Volatile.Write(ref messageQueuePlacementMode, (int)mode);
            Interlocked.Increment(ref messageQueuePolicyRevision);
        }
        if (notify) PendingSignalAdded428?.Invoke(null);

        return previous;
    }

    private bool? AdmitPolicyMoveMarker()
    {
        // Called by the REAL existing pending-signal owner under M/R.
        if (policyMovePending) policyMoveReady = true;

        return null;
    }

    internal bool TryBeginMessageQueuePolicyDrain(bool afterGc)
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || policyMoveDraining) return false;
            if (afterGc ? MessageQueueData411 != ProcessMessageQueueDataMode411.OnHeap : !policyMoveReady)
                return false;
            policyMoveDraining = true;

            return true;
        }
    }

    internal bool HasUnconvertedTokenPolicyRoots(ProcessTermHeap506 processHeap, bool toOffHeap)
    {
        lock (gate)
            foreach (var entry in messages)
                if (!entry.Control && entry.Admission is null && !entry.Deferred
                    && (entry.TraceToken is not null || entry.DistributionTraceToken is not (null or Nil))
                    && (entry.Custody593?.RecipientFragment is not { } fragment
                        || (toOffHeap ? ReferenceEquals(fragment.HeapOwner, processHeap) : fragment.OwnsHeap)))
                    return true;

        return false;
    }

    internal void PauseUnsupportedTokenPolicyMove()
    {
        // A later token-frame producer amendment must rearm this work. Keep
        // pending/effective OFF; never manufacture a completed move or spin.
        lock (gate) policyMoveReady = false;
    }

    internal void EndMessageQueuePolicyDrain()
    {
        lock (gate) policyMoveDraining = false;
    }

    internal void CompleteMessageQueuePolicyMove()
    {
        lock (gate)
        {
            policyMoveReady = false;
            policyMovePending = false;
            policyMoveScan.AfterNode = null;
            policyMoveScan.Revision = mutationRevision;
            Volatile.Write(ref messageQueuePlacementMode, (int)MessageQueueData411);
            // Invalidates preparation captured while OFF remained effective.
            Interlocked.Increment(ref messageQueuePolicyRevision);
        }
    }

    internal MailboxPolicyMoveClaim? ClaimMessageQueuePolicyMove(ProcessTermHeap506 processHeap, bool toOffHeap, MailboxPolicyScan scan)
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593)) return null;
            var start = scan.Revision == mutationRevision && scan.AfterNode is LinkedListNode<Entry> previous
                && ReferenceEquals(previous.List, messages) ? previous.Next : messages.First;
            for (var node = start; node is not null; node = node.Next)
            {
                Entry entry = node.Value;
                if (entry.Control || entry.Admission is not null || entry.Deferred || entry.ReceiveMarker is not null
                    || entry.Custody593 is not { } custody || custody.RecipientFragment is not { } fragment)
                    continue;
                bool processOwned = ReferenceEquals(fragment.HeapOwner, processHeap);
                if (toOffHeap ? !processOwned : !fragment.OwnsHeap) continue;

                return new(
                    node,
                    entry.Value,
                    custody,
                    custody.BorrowOperation(),
                    scan,
                    mutationRevision,
                    entry.TraceToken,
                    entry.DistributionTraceToken
                );
            }

            return null;
        }
    }

    // Caller holds R, then this method takes M. No native getters/callbacks.
    internal bool CommitMessageQueuePolicyMove(
        MailboxPolicyMoveClaim claim,
        PreparedMailboxMessage prepared,
        MailboxPublicationScope593 publication,
        long revision,
        bool toOffHeap,
        Term? portableRaw
    )
    {
        lock (gate)
        {
            if (Volatile.Read(ref closed593) || MessageQueuePolicyRevision != revision
                || MessageQueueData411 != (toOffHeap ? ProcessMessageQueueDataMode411.OffHeap : ProcessMessageQueueDataMode411.OnHeap)
                || claim.Node is not LinkedListNode<Entry> node || !ReferenceEquals(node.List, messages)
                || !ReferenceEquals(node.Value.Value, claim.Value) || !ReferenceEquals(node.Value.Custody593, claim.Custody)
                || !ReferenceEquals(node.Value.TraceToken, claim.Token) || !ReferenceEquals(node.Value.DistributionTraceToken, claim.RawToken)
                || node.Value.Control || node.Value.Admission is not null || node.Value.Deferred)
                return false;
            MailboxEntryCustody593? next = PrepareCustody593(publication);
            if (next is null) throw new InvalidOperationException(MessageQueuePolicyParameters.Invalid);
            bool uncontendedScan = mutationRevision == claim.ScanRevision;
            node.Value = node.Value with
            {
                Value = prepared.Value,
                TraceToken = prepared.Token,
                DistributionTraceToken = portableRaw,
                PreserveTrace = portableRaw is not null && prepared.Token is not null ? false : node.Value.PreserveTrace,
                Custody593 = next
            };
            next.Publish();
            mutationRevision++;
            precedenceRevision++;
            claim.Scan.AfterNode = uncontendedScan ? node : null;
            claim.Scan.Revision = mutationRevision;
            claim.Custody.Dispose(); // Latch/schedule only, held source operation stays live.

            return true;
        }
    }
}
