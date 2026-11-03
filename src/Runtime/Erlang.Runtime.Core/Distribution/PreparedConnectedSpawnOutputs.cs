namespace Erlang;

internal sealed class PreparedConnectedSpawnOutputs(
    PreparedSequenceTraceOutput? header,
    PreparedSequenceTraceOutput? reply,
    bool receive,
    bool send
) : IDisposable
{
    internal PreparedSequenceTraceOutput? Header { get; } = header;
    internal PreparedSequenceTraceOutput? Reply { get; } = reply;

    internal bool IsCurrent(SequenceTracing tracing, bool hasToken)
        => !hasToken || (!receive || tracing.IsCurrentOutputPlan(Header)) && (!send || tracing.IsCurrentOutputPlan(Reply));

    public void Dispose()
    {
        Exception? first = null;
        try
        {
            if (Header is not null) Header.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            first = error;
        }
        try
        {
            if (Reply is not null) Reply.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            if (first is not null) throw new AggregateException(first, error);
            throw;
        }
        if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
    }
}
