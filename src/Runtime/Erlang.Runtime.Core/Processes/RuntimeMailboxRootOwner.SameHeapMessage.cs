namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    internal async ValueTask<PreparedMailboxMessage> PrepareSameHeapMessageAsync(ProcessContext context, Term source)
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
            // This route intentionally avoids general RetainGraph524: that
            // route correctly pins writable bits when actually shared/exported.
            work.Attach(NativeResourceRoots524.RetainSameHeapObservation(context, source));
            Term observation = OwnedTermDetachedCopy506.CopySameHeapMailboxObservation(context.Runtime, source);
            MailboxMessageFragment fragment = await MailboxMessageFragment.RetainSameHeapAsync(context, source)
                .ConfigureAwait(false);
            work.AttachRecipientPreparation(fragment, context.Mailbox.PayloadRetirements593);
            var graph = new MailboxGraphSnapshot593(context.Runtime, work, observation);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(graph, source);
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
