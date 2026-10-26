namespace Erlang;

internal sealed class AllocatorQueryReplyReducer
{
    private readonly AllocatorQueryRequest request;
    private readonly ReferenceTerm reference;
    private Term accumulated;

    internal int Remaining
    {
        get; private set;
    }

    internal AllocatorQueryReplyReducer(
        AllocatorQueryRequest request,
        ReferenceTerm reference,
        int producerReplyCount
    )
    {
        if (producerReplyCount < 0)
            throw new ArgumentOutOfRangeException(nameof(producerReplyCount), AllocatorQueryNames.NegativeReplyCount);

        this.request = request;
        this.reference = reference;
        accumulated = AllocatorQueryResultReducer.Initial(request);
        Remaining = producerReplyCount;
    }

    internal bool TryAccept(Term message)
    {
        if (Remaining == 0 || message is not TupleTerm { Items.Count: 3 } reply || !reply.Items[0].Equals(reference))
            return false;

        Term next = AllocatorQueryResultReducer.Merge(reply.Items[2], accumulated);
        accumulated = next;
        Remaining--;

        return true;
    }

    internal Term Result
    {
        get
        {
            if (Remaining != 0)
                throw new InvalidOperationException(AllocatorQueryNames.Incomplete);

            return AllocatorQueryResultReducer.Finish(request, accumulated);
        }
    }
}
