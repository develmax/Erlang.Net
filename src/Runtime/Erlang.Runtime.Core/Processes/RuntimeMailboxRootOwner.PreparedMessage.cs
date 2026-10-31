namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    // Registered before native retention/import. Root shutdown joins preparation.
    internal async ValueTask<PreparedMailboxMessage> PrepareMailboxMessageAsync(
        ProcessRuntime runtime,
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        Term source
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
            work.Attach(NativeResourceRoots524.RetainGraph524(source, runtime));
            bool plainPortable = MailboxPortableInput.CanReuse(source);
            // Import still copies every physical occurrence. An immutable plain
            // portable input already IS the DTO; expanding its shared DAG into
            // another DTO first only duplicates work and temporary objects.
            Term portable = plainPortable ? source : OwnedTermDetachedCopy506.CopyAtomics586(runtime, source,
                exportHostAtomics586: false);
            MailboxMessageFragment fragment = await MailboxMessageFragment.CreatePreparedAsync(runtime,
                mode == ProcessMessageQueueDataMode411.OnHeap ? recipient : null, portable).ConfigureAwait(false);
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
