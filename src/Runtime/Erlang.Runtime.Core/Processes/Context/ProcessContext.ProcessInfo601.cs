using System.Runtime.ExceptionServices;

namespace Erlang;

public sealed partial class ProcessContext
{
    internal async ValueTask<Term> ReceiveProcessInfoReply601(ReferenceTerm id)
    {
        var matcher = new ProcessInfoReplyMatcher601(this, id);
        MailboxSelectionAdmission593 admission = MailboxSelectionAdmission593.ProcessInfoReply601(this, matcher);
        ProcessInfoReplySelection601? selected = null;
        Exception? primary = null;
        try
        {
            selected = await ReceiveAdmittedSelecting593(
                value => admission.InvokeProcessInfo601(matcher, value), null, admission);
            Runtime.ThrowIfHalted();
            Cancellation.ThrowIfCancellationRequested();
            if (selected is null) throw new InvalidOperationException(MailboxSnapshot601Names.InvalidAdmission);
            // The received origin interest remains held until result publication.
            var captured = new ProcessInfoSnapshot601 { Value = selected.Tuple.Items[1] };

            return captured.Complete(Runtime, this);
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
                selected?.Dispose();
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }

    private sealed class ProcessInfoReplyMatcher601(ProcessContext context, ReferenceTerm id)
        : IProcessInfoMailboxSelector601<ProcessInfoReplySelection601>
    {
        public ProcessContext Context => context;

        public ValueTask<ProcessInfoReplySelection601?> Select(Term value)
            => ValueTask.FromResult(value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(id)
                ? new ProcessInfoReplySelection601(tuple) : null);
    }

    private sealed class ProcessInfoReplySelection601(TupleTerm tuple)
        : IGuestMailboxSelection593, IProtocolMailboxSelection601
    {
        private IMailboxRootInterest593? interest;
        private bool attached;
        internal TupleTerm Tuple => tuple;

        public void AttachSnapshotInterest601(IMailboxRootInterest593? value)
        {
            if (attached) throw new InvalidOperationException(MailboxSnapshot601Names.DoubleSelection);
            interest = value;
            attached = true;
        }

        public void AttachMailboxCandidate593(ProcessTermFrame506 candidate)
            => throw new InvalidOperationException(MailboxSnapshot601Names.InvalidAdmission);

        public void Dispose() => MailboxRootSettlement593.Settle(Interlocked.Exchange(ref interest, null)?.ReleaseLast());
    }
}
