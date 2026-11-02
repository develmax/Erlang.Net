namespace Erlang;

internal sealed class ReadonlyTracePrefix817 : IAsyncDisposable
{
    internal IReadOnlyList<ReadonlyTracePrefixNode817> Nodes { get; }
    internal long Revision { get; }
    internal long PolicyRevision { get; }

    internal ReadonlyTracePrefix817(IReadOnlyList<ReadonlyTracePrefixNode817> nodes, long revision, long policyRevision)
    {
        Nodes = Array.AsReadOnly(nodes.ToArray());
        Revision = revision;
        PolicyRevision = policyRevision;
    }

    internal async ValueTask<bool> Prepare(ProcessRuntime runtime, ProcessContext receiver, ProcessContext tracer)
    {
        foreach (var item in Nodes)
            if (item.Source is { } source)
            {
                if (!source.ValidateOutsideGates()) return false;
                item.Output = await runtime.MailboxRoots593.ReserveReadonlyTraceOutput817Async(receiver, tracer, source).ConfigureAwait(false);
            }

        return true;
    }

    internal static void ReleaseSourceOperations817(IEnumerable<ReadonlyTracePrefixNode817> nodes, Exception? primary)
    {
        List<Exception>? failures = null;
        foreach (var item in nodes)
            try
            {
                item.Source?.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        if (failures is not null)
        {
            if (primary is not null) failures.Insert(0, primary);
            throw new AggregateException(failures);
        }
    }

    public async ValueTask DisposeAsync()
    {
        List<Exception>? failures = null;
        foreach (var item in Nodes)
        {
            try
            {
                if (item.Output is { } output) await output.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
            try
            {
                item.Source?.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
