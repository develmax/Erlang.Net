namespace Erlang;

// Local trusted publication transaction, passed explicitly through send/trace
// builders. It is not ambient authority and is not a public callback lease.
internal sealed partial class MailboxPublicationScope593 : IDisposable, IAsyncDisposable
{
    private readonly LinkedList<MailboxPayloadLease593> works = new();
    private readonly IDisposable? sender;
    private IMailboxRootInterest593? borrowedGraph;
    private bool ownsProducer;
    private int disposed;
    internal ProcessRuntime Runtime { get; }
    internal MailboxGraphSnapshot593? Graph { get; private set; }

    private MailboxPublicationScope593(ProcessRuntime runtime, IDisposable? sender)
    {
        Runtime = runtime;
        this.sender = sender;
    }

    // Mandatory before runtime/mailbox monitors. Includes the complete native
    // graph of the message and any explicitly admitted secondary trace inputs.
    internal static MailboxPublicationScope593 Capture(
        ProcessRuntime runtime, Term completeGraph, IDisposable? sender = null
    )
    {
        MailboxPublicationScope593? result = null;
        try
        {
            result = new(runtime, sender);
            result.Graph = runtime.MailboxRoots593.PrepareGraph(runtime, completeGraph);
            result.ownsProducer = true;

            return result;
        }
        catch (Exception original)
        {
            try
            {
                if (result is not null) result.Dispose();
                else sender?.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    // Caller holds an actual Entry operation interest until this nested scope
    // completes. Trace forwarding therefore forks explicit, covered roots.
    internal static MailboxPublicationScope593 Forward(
        ProcessRuntime runtime, MailboxGraphSnapshot593? graph
    )
    {
        var result = new MailboxPublicationScope593(runtime, null);
        if (graph is not null && !ReferenceEquals(graph.Runtime, runtime))
            throw new NotSupportedException(ProcessTerm506Names.ForeignRoot);

        result.Graph = graph;
        result.borrowedGraph = (IMailboxRootInterest593?)graph?.Retain();

        return result;
    }

    internal void Track(MailboxPayloadLease593 work)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        works.AddLast(work.PublicationLink593);
    }

    public void Dispose() => DisposeCore593();

    public ValueTask DisposeAsync()
    {
        DisposeCore593();

        return ValueTask.CompletedTask;
    }

    private void DisposeCore593()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        // Cleanup FIRST without allocating error containers. Each work already
        // has a reference field for failure capture; final aggregation happens
        // only after every independent lease/producer/sender cleanup attempted.
        Exception? graphFailure = null, senderFailure = null;
        foreach (MailboxPayloadLease593 work in works)
        {
            if (work.Published593 || !work.Registered593)
                continue;

            try
            {
                work.ReleaseOwner();
                MailboxRootSettlement593.Settle(work);
            }
            catch (Exception error)
            {
                work.ScopeCleanupFailure593 = error;
            }
        }
        try
        {
            if (ownsProducer && Graph is not null)
                Graph.DisposeAsync().GetAwaiter().GetResult();
            else
                MailboxRootSettlement593.Settle(Interlocked.Exchange(ref borrowedGraph, null)?.ReleaseLast());
        }
        catch (Exception error)
        {
            graphFailure = error;
        }
        try
        {
            sender?.Dispose();
        }
        catch (Exception error)
        {
            senderFailure = error;
        }

        Exception[] failures = works.Where(work => !work.Published593 && work.Registered593)
            .SelectMany(work => work.Failures.Append(work.ScopeCleanupFailure593).OfType<Exception>())
            .Concat(new[] { graphFailure, senderFailure }.OfType<Exception>())
            .Distinct<Exception>(ReferenceEqualityComparer.Instance).ToArray();
        if (failures.Length != 0)
            throw new AggregateException(failures);
    }
}
