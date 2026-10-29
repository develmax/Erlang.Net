namespace Erlang;

// Retains only already-authenticated complete graph interests. No source walk,
// native recipe factory or arbitrary callback executes during Capture641.
internal sealed class RuntimeProcessPayloadGroup641 : IDisposable
{
    private IMailboxRootInterest593? first, second;

    internal void Capture641(MailboxGraphSnapshot593? left, MailboxGraphSnapshot593? right)
    {
        first = (IMailboxRootInterest593?)left?.Retain();
        second = (IMailboxRootInterest593?)right?.Retain();
    }

    public void Dispose()
    {
        Exception? left = null, right = null;
        try
        {
            MailboxRootSettlement593.Settle(Interlocked.Exchange(ref first, null)?.ReleaseLast());
        }
        catch (Exception error)
        {
            left = error;
        }
        try
        {
            MailboxRootSettlement593.Settle(Interlocked.Exchange(ref second, null)?.ReleaseLast());
        }
        catch (Exception error)
        {
            right = error;
        }
        if (left is not null && right is not null) throw new AggregateException(left, right);
        if (left is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(left).Throw();
        if (right is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(right).Throw();
    }
}
