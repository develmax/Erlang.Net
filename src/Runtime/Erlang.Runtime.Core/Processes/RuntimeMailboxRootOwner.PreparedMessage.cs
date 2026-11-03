namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    // Token and message acquire independent native custody together, then two
    // physical root slots in one fragment. The portable wrapper is never
    // imported and contributes no imaginary process heap allocation.
    internal async ValueTask<PreparedMailboxMessage> PrepareMailboxMessageAsync(
        ProcessRuntime runtime,
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        Term source,
        SequenceTraceToken token,
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
            using ProcessTermBorrow506? sourceBorrow = retainedSource?.BorrowPreparationSource(
                runtime,
                source,
                token,
                null
            );
            Term complete = Term.Tuple(source, token.ToTerm());
            using ModuleLiteralRoots801? physicalLiteral = SequenceTraceLiteral814.CapturePhysical(runtime, token);
            using ModuleLiteralRoots801? carried = ModuleLiteralRoots801.RetainCombined801([carriedLiteral801, physicalLiteral]);
            ProcessContext? literalActor = RuntimeActorExecutionTurn.CurrentLiteralContext801(runtime);
            ModuleLiteralRoots801? literalRoots = sourceBorrow is not null
                ? ModuleLiteralRoots801.Capture801(sourceBorrow.Owner, complete, carried)
                : literalActor is null ? carried?.Retain801()
                : ModuleLiteralRoots801.Capture801(literalActor.TermHeap506, complete, carried);
            work.AttachLiteralRoots801(literalRoots);
            work.Attach(NativeResourceRoots524.RetainGraph524(complete, runtime));
            var portable = (TupleTerm)OwnedTermDetachedCopy506.CopyGuestLiteral801(runtime, complete, literalRoots);
            Term portableMessage = portable.Items[0];
            SequenceTraceToken portableToken = SequenceTraceToken.FromPortable((TupleTerm)portable.Items[1]);
            MailboxMessageFragment fragment = await MailboxMessageFragment.CreatePreparedToken814Async(
                runtime,
                mode == ProcessMessageQueueDataMode411.OnHeap ? recipient : null,
                portableMessage,
                portableToken,
                literalRoots
            ).ConfigureAwait(false);
            work.AttachRecipientPreparation(fragment, recipient.Mailbox.PayloadRetirements593);
            Term observation = MailboxValueEscape.RequiresDetach(source) ? portableMessage : source;
            var graph = new MailboxGraphSnapshot593(runtime, work, observation);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(graph, fragment.Value, fragment.Token);
        }
        catch (Exception original)
        {
            work.ReleaseOwner();
            try
            {
                await MailboxRootSettlement593.SettleAsync(work).ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    // Registered before native retention/import. Root shutdown joins preparation.
    internal ValueTask<PreparedMailboxMessage> PrepareMailboxMessageAsync(
        ProcessRuntime runtime,
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        Term source
    ) => PrepareMailboxMessageWithLiteral824Async(
        runtime,
        recipient,
        mode,
        source,
        null
    );

    internal async ValueTask<PreparedMailboxMessage> PrepareMailboxMessageWithLiteral824Async(
        ProcessRuntime runtime,
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        Term source,
        ModuleLiteralRoots801? carriedLiteral801,
        MailboxPendingBackingClaim? retainedSource = null
    )
    {
        var work = new MailboxPayloadLease593(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.Link);
            work.MarkRegistered593();
        }
        try
        {
            using ProcessTermBorrow506? sourceBorrow = retainedSource?.BorrowPreparationSource(
                runtime,
                source,
                null,
                null
            );
            ProcessContext? literalActor = RuntimeActorExecutionTurn.CurrentLiteralContext801(runtime);
            ModuleLiteralRoots801? literalRoots = sourceBorrow is not null
                ? ModuleLiteralRoots801.Capture801(sourceBorrow.Owner, source, carriedLiteral801)
                : literalActor is null ? carriedLiteral801?.Retain801()
                : ModuleLiteralRoots801.Capture801(literalActor.TermHeap506, source, carriedLiteral801);
            work.AttachLiteralRoots801(literalRoots);
            work.Attach(NativeResourceRoots524.RetainGraph524(source, runtime));
            bool plainPortable = MailboxPortableInput.CanReuse(source);
            // Import still copies every physical occurrence. An immutable plain
            // portable input already IS the DTO; expanding its shared DAG into
            // another DTO first only duplicates work and temporary objects.
            Term portable = plainPortable ? source : OwnedTermDetachedCopy506.CopyGuestLiteral801(runtime, source, literalRoots);
            MailboxMessageFragment fragment = await MailboxMessageFragment.CreatePreparedWithLiteral801Async(
                runtime,
                mode == ProcessMessageQueueDataMode411.OnHeap ? recipient : null,
                portable,
                literalRoots
            ).ConfigureAwait(false);
            work.AttachRecipientPreparation(fragment, recipient.Mailbox.PayloadRetirements593);
            Term observation = !plainPortable && MailboxValueEscape.RequiresDetach(source) ? portable : source;
            var graph = new MailboxGraphSnapshot593(runtime, work, observation);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(graph, fragment.Value);
        }
        catch (Exception original)
        {
            work.ReleaseOwner();
            try
            {
                await MailboxRootSettlement593.SettleAsync(work).ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}
