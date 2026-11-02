namespace Erlang;

internal sealed partial class MailboxPublicationScope593
{
    // Only a Forward scope: release/schedule exact rejected work under R,
    // never execute its disposal or synchronously join native callbacks here.
    internal void ReleaseForwardDeferred172()
    {
        if (ownsProducer || sender is not null) throw new InvalidOperationException();
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Exception? first = null;
        foreach (MailboxPayloadLease593 work in works)
        {
            work.MarkSuspensionAckCleanup172();
            if (work.Published593 || !work.Registered593) continue;
            try
            {
                work.ReleaseOwner();
            }
            catch (Exception failure)
            {
                work.ScopeCleanupFailure593 = failure;
                first ??= failure;
            }
        }
        try
        {
            borrowedGraph?.Dispose();
        }
        catch (Exception failure)
        {
            first ??= failure;
        }
        borrowedGraph = null;
        if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
    }
}
