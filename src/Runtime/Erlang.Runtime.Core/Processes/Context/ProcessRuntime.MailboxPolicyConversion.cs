namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ProcessMessageQueueDataMode411 ChangeMessageQueuePolicy(ProcessContext owner, ProcessMessageQueueDataMode411 mode)
    {
        lock (gate)
        {
            if (!ReferenceEquals(owner.Runtime, this) || owner.Terminated
                || !processes.TryGetValue(owner.Self, out var actual) || !ReferenceEquals(actual, owner))
                throw new OperationCanceledException();

            return owner.Mailbox.RequestMessageQueuePolicy(mode);
        }
    }

    private bool CanDrainMessageQueuePolicy(ProcessContext owner)
        => RuntimeActorExecutionTurn.IsOwnedBy(owner) && !System.Threading.Monitor.IsEntered(gate)
            && !owner.Mailbox.DeferredGateHeld && owner.DeferredSignalDrain?.IsCurrent != true
            && !owner.Terminated && !IsHaltRequested;

    private void DrainMessageQueuePoliciesSynchronously(ProcessContext owner)
    {
        if (!CanDrainMessageQueuePolicy(owner) || !owner.Mailbox.TryBeginMessageQueuePolicyDrain(false)) return;
        try
        {
            _ = MoveMessageQueuePolicyBatch(
                owner,
                false,
                MessageQueuePolicyParameters.EntriesPerBatch,
                owner.Mailbox.PolicyMoveScan
            );
        }
        finally
        {
            owner.Mailbox.EndMessageQueuePolicyDrain();
        }
    }

    internal async ValueTask DrainMessageQueuePoliciesAsync(ProcessContext owner)
    {
        if (!CanDrainMessageQueuePolicy(owner) || !owner.Mailbox.TryBeginMessageQueuePolicyDrain(false)) return;
        try
        {
            while (MoveMessageQueuePolicyBatch(
                owner,
                false,
                MessageQueuePolicyParameters.EntriesPerBatch,
                owner.Mailbox.PolicyMoveScan
            ))
            {
                // REAL admitted actor continuation, not ThreadPool work, fake
                // Ready or a metadata-only yield. No claim survives this await.
                bool priorYield = owner.Yielding;
                owner.Yielding = true;
                try
                {
                    await owner.YieldExecution();
                }
                finally
                {
                    owner.Yielding = priorYield;
                }
                if (!CanDrainMessageQueuePolicy(owner)) return;
            }
        }
        finally
        {
            owner.Mailbox.EndMessageQueuePolicyDrain();
        }
    }

    internal void ImportOnHeapMessagesAfterCollection(ProcessContext owner)
    {
        if (!CanDrainMessageQueuePolicy(owner) || !owner.Mailbox.TryBeginMessageQueuePolicyDrain(true)) return;
        try
        {
            // Pinned GC imports attached message fragments AFTER the actual
            // sweep. These newly allocated roots stay young; no second GC and
            // no fragment-disposal reclaimed-word credit is recorded here.
            var scan = new MailboxPolicyScan();
            while (MoveMessageQueuePolicyBatch(
                owner,
                true,
                MessageQueuePolicyParameters.EntriesPerBatch,
                scan
            )) { }
        }
        finally
        {
            owner.Mailbox.EndMessageQueuePolicyDrain();
        }
    }

    private bool MoveMessageQueuePolicyBatch(
        ProcessContext owner,
        bool afterGc,
        int budget,
        MailboxPolicyScan scan
    )
    {
        ProcessTermHeap506 processHeap = owner.TermHeap506;
        for (int count = 0; count < budget; count++)
        {
            MailboxPolicyMoveClaim? claim;
            long revision;
            bool toOffHeap;
            lock (gate)
            {
                if (owner.Terminated || IsHaltRequested || !processes.TryGetValue(owner.Self, out var actual)
                    || !ReferenceEquals(actual, owner)) return false;
                toOffHeap = owner.Mailbox.MessageQueueData411 == ProcessMessageQueueDataMode411.OffHeap;
                if (afterGc ? toOffHeap : !toOffHeap)
                {
                    if (!afterGc) owner.Mailbox.CompleteMessageQueuePolicyMove();

                    return false;
                }
                revision = owner.Mailbox.MessageQueuePolicyRevision;
                claim = owner.Mailbox.ClaimMessageQueuePolicyMove(processHeap, toOffHeap, scan);
                if (claim is null)
                {
                    if (!afterGc)
                    {
                        if (owner.Mailbox.HasUnconvertedTokenPolicyRoots(processHeap, true))
                            owner.Mailbox.PauseUnsupportedTokenPolicyMove();
                        else owner.Mailbox.CompleteMessageQueuePolicyMove();
                    }

                    return false;
                }
            }
            using (claim)
            {
                // Actual742 frame/root/copy construction and private callable
                // rebinding occur outside R/M/H while old custody stays held.
                PreparedMailboxMessage prepared;
                try
                {
                    prepared = mailboxRoots593.PrepareMailboxMessageAsync(
                        this,
                        owner,
                        toOffHeap ? ProcessMessageQueueDataMode411.OffHeap : ProcessMessageQueueDataMode411.OnHeap,
                        claim.Value
                    ).GetAwaiter().GetResult();
                }
                catch (ObjectDisposedException)
                {
                    lock (gate) if (owner.Terminated || IsHaltRequested) return false;
                    throw;
                }
                Exception? primary = null;
                try
                {
                    using var publication = MailboxPublicationScope593.Forward(this, prepared.Graph);
                    lock (gate)
                    {
                        if (!owner.Terminated && !IsHaltRequested && processes.TryGetValue(owner.Self, out var actual) && ReferenceEquals(actual, owner))
                            _ = owner.Mailbox.CommitMessageQueuePolicyMove(
                                claim,
                                prepared,
                                publication,
                                revision,
                                toOffHeap
                            );
                    }
                }
                catch (Exception error)
                {
                    primary = error;
                    throw;
                }
                finally
                {
                    try
                    {
                        prepared.DisposeAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception cleanup) when (primary is not null && !ReferenceEquals(primary, cleanup))
                    {
                        throw new AggregateException(primary, cleanup);
                    }
                }
            }
        }

        return true;
    }
}
