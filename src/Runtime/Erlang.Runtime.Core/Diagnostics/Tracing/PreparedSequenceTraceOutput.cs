namespace Erlang;

// Exactly one synchronous event. Arbitrary leaves and their native custody are
// prepared outside R; final scalar/time/local-PID/wrapper allocation is bounded
// and callback-free under H. Admission remains at the original R-held call site.
internal sealed class PreparedSequenceTraceOutput : IAsyncDisposable
{
    private readonly MailboxGraphSnapshot593 graph;
    private readonly MailboxMessageFragment fragment;
    private readonly Dictionary<Term, Term> leaves;
    private readonly ProcessTermConstruction514 construction;
    private int bound;
    internal ProcessContext Recipient { get; }
    internal ProcessMessageQueueDataMode411 Mode { get; }
    internal long Policy { get; }

    internal PreparedSequenceTraceOutput(
        ProcessContext recipient,
        ProcessMessageQueueDataMode411 mode,
        long policy,
        MailboxGraphSnapshot593 graph,
        MailboxMessageFragment fragment,
        Dictionary<Term, Term> leaves,
        ProcessTermConstruction514 construction
    )
    {
        Recipient = recipient;
        Mode = mode;
        Policy = policy;
        this.graph = graph;
        this.fragment = fragment;
        this.leaves = leaves;
        this.construction = construction;
    }

    // Must revalidate captured recipient identity/mode/policy BEFORE token mutation.
    // Global tracer replacement does not retarget this prepared event.
    // This method does not defer, enqueue or alter admission ordering.
    internal PreparedMailboxMessage BindNotification(Term notification)
    {
        if (Interlocked.Exchange(ref bound, 1) != 0)
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        Term actual = fragment.BindSequenceTraceNotification(notification, leaves, construction);

        return new(graph, actual);
    }

    public ValueTask DisposeAsync() => graph.DisposeAsync();
}
