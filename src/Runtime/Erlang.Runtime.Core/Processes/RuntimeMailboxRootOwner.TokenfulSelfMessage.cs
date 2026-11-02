namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    // Genuine actor-local source and state are borrowed through this operation.
    // Both Value and token are copied, exactly as PIN's token!=NIL branch.
    internal async ValueTask<PreparedMailboxMessage> PrepareTokenfulSelfMessageAsync(
        ProcessContext context,
        Term source,
        SequenceTraceToken token
    )
    {
        context.Runtime.AssertSequenceTracePreparation();
        Term complete = Term.Tuple(source, token.ToTerm());
        using ModuleLiteralRoots801? literalRoots = ModuleLiteralRoots801.Capture801(context.TermHeap506, complete);
        lock (context.TermHeap506.Gate)
        {
            using var literalAdmission = context.TermHeap506.EnterLiteralAdmission801(literalRoots);
            context.TermHeap506.ValidateGuestGraph507(complete);
        }
        var work = new MailboxPayloadLease593(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            active.AddLast(work.Link);
            work.MarkRegistered593();
        }
        try
        {
            // Actual copied BinRefs below own/pin ordinary Binary backing.
            // An extra ordinary lease for a metadata DTO would inflate native
            // refcounts; other native families retain their normal custody.
            work.AttachLiteralRoots801(literalRoots?.Retain801());
            work.Attach(NativeResourceRoots524.RetainSameHeapObservation(context, complete));
            var portable = (TupleTerm)OwnedTermDetachedCopy506.CopyGuestLiteral801(
                context.Runtime, complete, literalRoots
            );
            SequenceTraceToken portableToken = SequenceTraceToken.FromPortable((TupleTerm)portable.Items[1]);
            MailboxMessageFragment fragment = await MailboxMessageFragment.CreatePreparedToken814Async(
                context.Runtime,
                context,
                portable.Items[0],
                portableToken,
                literalRoots
            ).ConfigureAwait(false);
            work.AttachRecipientPreparation(fragment, context.Mailbox.PayloadRetirements593);
            // Observation bytes are consumed INTERNAL metadata, not a Host/
            // store escape and not an additional interest in ordinary Binary.
            Term observation = OwnedTermDetachedCopy506.CopySameHeapMailboxObservation(context.Runtime, source);
            var graph = new MailboxGraphSnapshot593(context.Runtime, work, observation);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(graph, fragment.Value, fragment.Token);
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
