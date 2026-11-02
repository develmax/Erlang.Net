namespace Erlang;

// Retains only already-authenticated complete graph interests. No source walk,
// native recipe factory or arbitrary callback executes during Capture641.
internal sealed class RuntimeProcessPayloadGroup641 : IDisposable
{
    private IMailboxRootInterest593? first, second;
    private MailboxGraphSnapshot593? firstGraph, secondGraph;
    private int captured;

    internal ModuleLiteralRoots801? RetainLiteralRoots801()
        => ModuleLiteralRoots801.RetainCombined801([firstGraph?.LiteralRoots801, secondGraph?.LiteralRoots801]);

    internal void Capture641(MailboxGraphSnapshot593? left, MailboxGraphSnapshot593? right)
    {
        firstGraph = left;
        secondGraph = right;
        first = (IMailboxRootInterest593?)left?.Retain();
        second = (IMailboxRootInterest593?)right?.Retain();
        Volatile.Write(ref captured, 1);
    }

    // The unpublished641 record owns rollback until registration returns;
    // no legitimate caller can dispose this group during this handoff.
    // Caller holds the matching593 owner gate; only sealed metadata is read.
    internal bool HasLiveInterests593(RuntimeMailboxRootOwner593 owner)
    {
        if (Volatile.Read(ref captured) == 0)
            return false;
        bool left = Volatile.Read(ref first) is not null;
        bool right = Volatile.Read(ref second) is not null;

        return (left || right)
            && (!left || firstGraph is not null && owner.IsLiveGraphAnchor593(firstGraph))
            && (!right || secondGraph is not null && owner.IsLiveGraphAnchor593(secondGraph));
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
