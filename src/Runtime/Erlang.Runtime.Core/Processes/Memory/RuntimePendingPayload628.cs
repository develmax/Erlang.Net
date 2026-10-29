namespace Erlang;

// The runtime owns this portable system payload, independently of any source
// process frame. Backing retention alone would leave stale source wrappers.
internal sealed class RuntimePendingPayload628 : IAsyncDisposable
{
    internal TupleTerm Value { get; }
    internal MailboxGraphSnapshot593? Graph { get; }

    private RuntimePendingPayload628(TupleTerm value, MailboxGraphSnapshot593? graph)
    {
        Value = value;
        Graph = graph;
    }

    // Must run before R/M. Caller keeps its actual source invocation or borrow
    // alive throughout validation, graph retain and complete detached copying.
    internal static RuntimePendingPayload628 Prepare(ProcessRuntime runtime, TupleTerm source)
    {
        using var atoms = new AtomNameScope(runtime.AtomNames);
        MailboxGraphSnapshot593? graph = runtime.MailboxRoots593.PrepareGraph(runtime, source);
        try
        {
            var stable = (TupleTerm)OwnedTermDetachedCopy506.CopyAtomics586(
                runtime,
                source,
                exportHostAtomics586: false
            );

            return new(stable, graph);
        }
        catch (Exception original)
        {
            try
            {
                graph?.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    // Only an outside-monitor owner or detached retirement worker may settle.
    public ValueTask DisposeAsync() => Graph?.DisposeAsync() ?? ValueTask.CompletedTask;
}
