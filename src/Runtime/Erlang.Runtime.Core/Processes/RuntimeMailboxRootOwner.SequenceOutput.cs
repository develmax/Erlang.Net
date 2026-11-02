namespace Erlang;

internal sealed partial class RuntimeMailboxRootOwner593
{
    internal async ValueTask<PreparedSequenceTraceOutput> PrepareSequenceTraceOutputAsync(
        ProcessRuntime runtime,
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        long policy,
        Term[] sourceRoots
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
            Term complete = Term.Tuple(sourceRoots);
            work.Attach(NativeResourceRoots524.RetainGraph524(complete, runtime));
            var portable = (TupleTerm)OwnedTermDetachedCopy506.CopyAtomics586(runtime, complete, exportHostAtomics586: false);
            // Nil is an actual immediate root; no tuple skeleton/shadow words
            // are charged for the portable preparation envelope itself.
            MailboxMessageFragment fragment = await MailboxMessageFragment.CreatePreparedAsync(runtime,
                mode == ProcessMessageQueueDataMode411.OnHeap ? recipient : null, Nil.Value).ConfigureAwait(false);
            work.AttachRecipientPreparation(fragment, recipient.Mailbox.PayloadRetirements593);
            ProcessTermConstruction514 construction = fragment.BeginSequenceTraceConstruction();
            var leaves = new Dictionary<Term, Term>(ReferenceEqualityComparer.Instance);
            for (int index = 0; index < sourceRoots.Length; index++)
            {
                if (leaves.ContainsKey(sourceRoots[index])) continue;
                Term actual = fragment.ImportSequenceTraceLeaf(portable.Items[index]);
                construction.Admit(actual);
                leaves.Add(sourceRoots[index], actual);
            }
            var graph = new MailboxGraphSnapshot593(runtime, work);
            lock (gate) ObjectDisposedException.ThrowIf(closed, this);

            return new(
                recipient,
                mode,
                policy,
                graph,
                fragment,
                leaves,
                construction
            );
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
