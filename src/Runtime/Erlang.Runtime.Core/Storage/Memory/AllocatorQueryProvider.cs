namespace Erlang;

internal sealed class AllocatorQueryProvider
{
    private readonly Func<ProcessContext, ReferenceTerm, Term, Action<Term, Term>, CancellationToken, Task> producer;
    private readonly CancellationToken retirement;
    private readonly int replyCount;

    internal IReadOnlySet<string> KnownAllocators
    {
        get;
    }

    internal AllocatorQueryProvider(
        IReadOnlySet<string> knownAllocators,
        int replyCount,
        Func<ProcessContext, ReferenceTerm, Term, Action<Term, Term>, CancellationToken, Task> producer,
        CancellationToken retirement
    )
    {
        ArgumentNullException.ThrowIfNull(knownAllocators);
        ArgumentNullException.ThrowIfNull(producer);
        if (replyCount < 0)
            throw new ArgumentOutOfRangeException(nameof(replyCount), AllocatorQueryNames.NegativeReplyCount);

        KnownAllocators = new HashSet<string>(knownAllocators, StringComparer.Ordinal);
        this.replyCount = replyCount;
        this.producer = producer;
        this.retirement = retirement;
    }

    internal AllocatorQueryOperation Prepare(
        ProcessContext owner,
        ReferenceTerm reference,
        AllocatorQueryRequest request
    ) => new(
        owner,
        reference,
        replyCount,
        (post, cancellation) => producer(
            owner,
            reference,
            request.Allocators,
            post,
            cancellation
        ),
        retirement
    );
}
