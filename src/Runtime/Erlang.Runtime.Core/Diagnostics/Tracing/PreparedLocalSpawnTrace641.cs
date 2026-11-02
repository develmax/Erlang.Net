namespace Erlang;

internal sealed class PreparedLocalSpawnTrace641 : IDisposable
{
    private readonly SequenceTracing tracing;
    private readonly ProcessContext parent;
    private int disposed;
    internal SequenceTraceSendPreview Preview { get; }
    internal PreparedSequenceTraceTransport641? Transport641 { get; set; }
    internal SequenceSpawnDescription? Description { get; }
    internal Pid GroupLeader { get; }
    internal bool Asynchronous { get; }
    internal bool SuccessReply { get; }
    private readonly bool predicted;
    internal PreparedSequenceTraceOutput? RequestSend { get; set; }
    internal PreparedSequenceTraceOutput? RequestReceive { get; set; }
    internal PreparedSequenceTraceOutput? ReplySend { get; set; }
    internal PreparedSequenceTraceOutput? ReplyReceive { get; set; }

    internal PreparedLocalSpawnTrace641(
        SequenceTracing tracing,
        ProcessContext parent,
        SequenceTraceSendPreview preview,
        SequenceSpawnDescription? description,
        Pid groupLeader,
        bool asynchronous,
        bool successReply,
        bool predicted = false
    )
    {
        this.tracing = tracing;
        this.parent = parent;
        Preview = preview;
        Description = description;
        GroupLeader = groupLeader;
        Asynchronous = asynchronous;
        SuccessReply = successReply;
        this.predicted = predicted;
    }

    // The caller holds R and performs its original liveness checks first.
    // No child/reference/link/serial mutation may precede this validation.
    internal bool IsCurrent()
    {
        if (!tracing.IsCurrentLocalSpawnPreview641(parent, Preview, predicted))
            return false;
        if (Description is null || Transport641 is null)
            return true;
        if (!parent.GroupLeader.Equals(GroupLeader))
            return false;

        return AreOutputPlansCurrent();
    }

    internal bool AreOutputPlansCurrent()
    {
        if (Description is null || Transport641 is null)
            return true;
        SequenceTraceToken request = Preview.Next!;
        SequenceTraceToken reply = Transport641.Token;

        return tracing.IsCurrentOutputPlanOrSuppressed(
            RequestSend,
            request,
            SequenceTraceFlags.Send,
            parent.Self
        )
            && tracing.IsCurrentOutputPlanOrSuppressed(
                RequestReceive,
                request,
                SequenceTraceFlags.Receive,
                parent.Self
            )
            && (!SuccessReply || tracing.IsCurrentOutputPlanOrSuppressed(
                ReplySend,
                reply,
                SequenceTraceFlags.Send,
                parent.Self
            )
                && (Asynchronous || tracing.IsCurrentOutputPlanOrSuppressed(
                    ReplyReceive,
                    reply,
                    SequenceTraceFlags.Receive,
                    parent.Self
                )));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        Exception? requestSend = null, requestReceive = null, replySend = null, replyReceive = null, transport = null;
        try
        {
            if (RequestSend is not null) RequestSend.DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            requestSend = error;
        }
        try
        {
            if (RequestReceive is not null) RequestReceive.DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            requestReceive = error;
        }
        try
        {
            if (ReplySend is not null) ReplySend.DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            replySend = error;
        }
        try
        {
            if (ReplyReceive is not null) ReplyReceive.DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            replyReceive = error;
        }
        try
        {
            Transport641?.Dispose();
        }
        catch (Exception error)
        {
            transport = error;
        }
        // Allocate failure aggregation only after all real owners were ended.
        Exception[] failures = new[] { requestSend, requestReceive, replySend, replyReceive, transport }.OfType<Exception>().ToArray();
        if (failures.Length != 0)
            throw new AggregateException(failures);
    }
}
