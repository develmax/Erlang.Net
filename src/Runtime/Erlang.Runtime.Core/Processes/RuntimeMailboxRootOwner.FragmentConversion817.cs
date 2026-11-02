namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    // Exact queued-fragment conversion only. The ordinary bare-term producer
    // remains unchanged and still rejects foreign process-heap storage.
    internal async ValueTask<PreparedMailboxMessage> PrepareMailboxMessageFromEntry817Async(
        ProcessRuntime runtime,
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        Term source,
        MailboxEntryCustody593 custody
    )
    {
        runtime.AssertSequenceTracePreparation();
        if (recipient.Mailbox.DeferredGateHeld || !ReferenceEquals(recipient.Runtime, runtime)
            || !custody.IsRecipient817(recipient.Mailbox.PayloadRetirements593))
            throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
        var work = new MailboxPayloadLease593(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.Link);
            work.MarkRegistered593();
        }
        IDisposable? operation = null;
        ProcessTermBorrow506? borrow = null;
        Exception? failure = null;
        try
        {
            operation = custody.BorrowOperation();
            MailboxMessageFragment actual = custody.RecipientFragment
                ?? throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
            if (!ReferenceEquals(actual.Value, source) || !ReferenceEquals(actual.HeapOwner.Runtime524, runtime))
                throw new InvalidOperationException(ProcessTerm506Names.ForeignRoot);
            borrow = actual.BorrowValueForConversion817(source);
            ModuleLiteralRoots801? literalRoots = ModuleLiteralRoots801.Capture801(actual.HeapOwner, source,
                custody.Graph?.LiteralRoots801);
            work.AttachLiteralRoots801(literalRoots);
            // Preserve the existing generic producer's real input backing.
            // This fix does not waive its separately recorded duplicate-anchor debt.
            work.Attach(NativeResourceRoots524.RetainGraph524(source, runtime));
            Term portable = OwnedTermDetachedCopy506.CopyGuestLiteral801(runtime, source, literalRoots);
            MailboxMessageFragment target = await MailboxMessageFragment.CreatePreparedWithLiteral801Async(
                runtime,
                mode == ProcessMessageQueueDataMode411.OnHeap ? recipient : null,
                portable,
                literalRoots
            ).ConfigureAwait(false);
            work.AttachRecipientPreparation(target, recipient.Mailbox.PayloadRetirements593);
            Term observation = MailboxValueEscape.RequiresDetach(source) ? portable : source;
            var graph = new MailboxGraphSnapshot593(runtime, work, observation);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);
            // Input cleanup failure still enters the normal target-work unwind;
            // it cannot strand a successfully constructed unpublished graph.
            ReleaseInput(null);

            return new(graph, target.Value);
        }
        catch (Exception original)
        {
            failure = original;
            work.ReleaseOwner();
            try
            {
                await MailboxRootSettlement593.SettleAsync(work).ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                failure = new AggregateException(original, cleanup);
                throw failure;
            }
            throw;
        }
        finally
        {
            // Keep actual source wrappers and native pointers live through
            // complete target preparation AND any failure cleanup.
            ReleaseInput(failure);
        }

        void ReleaseInput(Exception? primary)
        {
            ProcessTermBorrow506? heldBorrow = Interlocked.Exchange(ref borrow, null);
            IDisposable? heldOperation = Interlocked.Exchange(ref operation, null);
            MailboxReceiveCleanup593.Dispose(
                heldBorrow,
                null,
                heldOperation,
                primary
            );
        }
    }
}
