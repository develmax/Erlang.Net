namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    internal async ValueTask<PreparedRawDistributionMailboxMessage> PrepareRawDistributionMailboxMessageAsync(
        ProcessRuntime runtime,
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        Term source,
        Term rawToken,
        ModuleLiteralRoots801? carriedLiteral801 = null,
        MailboxPendingBackingClaim? retainedSource = null
    )
    {
        runtime.AssertSequenceTracePreparation();
        var work = new MailboxPayloadLease593(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.Link);
            work.MarkRegistered593();
        }
        try
        {
            // Wrapper exists only for portable/native preparation. Actual
            // import uses TWO root slots, never wrapper tuple heap words.
            using ProcessTermBorrow506? sourceBorrow = retainedSource?.BorrowPreparationSource(
                runtime,
                source,
                retainedSource.Original.TraceToken,
                rawToken
            );
            Term complete = Term.Tuple(source, rawToken);
            ProcessContext? literalActor = RuntimeActorExecutionTurn.CurrentLiteralContext801(runtime);
            ModuleLiteralRoots801? literalRoots = sourceBorrow is not null
                ? ModuleLiteralRoots801.Capture801(sourceBorrow.Owner, complete, carriedLiteral801)
                : literalActor is null ? carriedLiteral801?.Retain801()
                : ModuleLiteralRoots801.Capture801(literalActor.TermHeap506, complete, carriedLiteral801);
            work.AttachLiteralRoots801(literalRoots);
            work.Attach(NativeResourceRoots524.RetainGraph524(complete, runtime));
            var portable = (TupleTerm)OwnedTermDetachedCopy506.CopyGuestLiteral801(runtime, complete, literalRoots);
            Term message = portable.Items[0], raw = portable.Items[1];
            MailboxMessageFragment fragment = await MailboxMessageFragment.CreatePreparedRawDistributionAsync(
                runtime,
                mode == ProcessMessageQueueDataMode411.OnHeap ? recipient : null,
                message,
                raw,
                literalRoots
            ).ConfigureAwait(false);
            work.AttachRecipientPreparation(fragment, recipient.Mailbox.PayloadRetirements593);
            var graph = new MailboxGraphSnapshot593(runtime, work, message);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(new(graph, fragment.Value, fragment.Token), raw);
        }
        catch (Exception primary)
        {
            work.ReleaseOwner();
            try
            {
                await MailboxRootSettlement593.SettleAsync(work).ConfigureAwait(false);
            }
            catch (Exception cleanup) when (!ReferenceEquals(primary, cleanup))
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }
}
