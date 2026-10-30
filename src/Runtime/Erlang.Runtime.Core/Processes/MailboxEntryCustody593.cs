namespace Erlang;

// Each physical Entry owns a distinct handle. Deferred copies in this SAME
// mailbox may share work, but never the handle's disposal latch.
internal sealed class MailboxEntryCustody593 : IDisposable
{
    private readonly MailboxPayloadLease593 work;
    private IDisposable? borrowed;
    private int released;
    private readonly bool original;

    internal MailboxEntryCustody593(MailboxPayloadLease593 work)
    {
        this.work = work;
        original = true;
    }

    private MailboxEntryCustody593(MailboxPayloadLease593 work, bool original)
    {
        this.work = work;
        this.original = original;
    }

    internal MailboxGraphSnapshot593? Graph => work.Graph;
    internal Term? ObservationValue => work.ObservationValue;

    internal IDisposable BorrowOperation() => work.BorrowOperation();

    internal void Publish() => work.MarkPublished593();

    internal MailboxEntryCustody593 CloneForSameMailbox()
    {
        var result = new MailboxEntryCustody593(work, false);
        result.borrowed = work.BorrowOperation();

        return result;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref released, 1) != 0)
            return;

        if (original)
            work.ReleaseOwner();
        else
            Interlocked.Exchange(ref borrowed, null)?.Dispose();
    }
}
