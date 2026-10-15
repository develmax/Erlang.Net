namespace Erlang;

internal sealed class AllocatorQueryOperation : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ProcessContext owner;
    private readonly ReferenceTerm reference;
    private readonly Func<Action<Term, Term>, CancellationToken, Task> producer;
    private readonly CancellationTokenSource cancellation;
    private readonly TaskCompletionSource completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource? postsDrained;
    private TaskCompletionSource? disposed;
    private bool started;
    private bool ended;
    private bool closing;
    private int reserved;
    private int posted;
    private int activePosts;
    private Exception? producerFailure;
    private Exception? postFailure;

    internal int ReplyCount
    {
        get;
    }
    internal CancellationToken Cancellation => cancellation.Token;
    internal Task Completion => completed.Task;
    internal Exception? ProducerFailure
    {
        get
        {
            lock (gate)
                return producerFailure;
        }
    }

    internal AllocatorQueryOperation(
        ProcessContext owner,
        ReferenceTerm reference,
        int replyCount,
        Func<Action<Term, Term>, CancellationToken, Task> producer,
        CancellationToken retirement
    )
    {
        this.owner = owner;
        this.reference = reference;
        this.producer = producer;
        ReplyCount = replyCount;
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(owner.Cancellation, retirement);
    }

    internal void Start()
    {
        lock (gate)
        {
            if (closing)
                throw new InvalidOperationException(AllocatorQueryMailboxNames.Closed);
            if (started)
                throw new InvalidOperationException(AllocatorQueryMailboxNames.AlreadyStarted);

            started = true;
        }
        _ = Produce();
    }

    private void Post(Term marker, Term information)
    {
        lock (gate)
        {
            if (!started || ended || closing)
                throw new InvalidOperationException(AllocatorQueryMailboxNames.Closed);
            cancellation.Token.ThrowIfCancellationRequested();
            if (reserved == ReplyCount)
            {
                var failure = new InvalidOperationException(AllocatorQueryMailboxNames.ExcessReplies);
                postFailure ??= failure;

                throw failure;
            }

            reserved++;
            if (activePosts++ == 0)
                postsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        try
        {
            owner.Runtime.SendSystem(
                owner.Self,
                Term.Tuple(reference, marker, information),
                Term.A(ProcessTraceNames.System)
            );
            lock (gate)
                posted++;
        }
        catch (Exception failure)
        {
            lock (gate)
                postFailure ??= failure;
            throw;
        }
        finally
        {
            TaskCompletionSource? drained;
            lock (gate)
            {
                activePosts--;
                drained = activePosts == 0 ? postsDrained : null;
            }
            drained?.TrySetResult();
        }
    }

    private async Task Produce()
    {
        Exception? failure = null;
        try
        {
            cancellation.Token.ThrowIfCancellationRequested();
            await producer(Post, cancellation.Token).ConfigureAwait(false);
            cancellation.Token.ThrowIfCancellationRequested();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        Task drain;
        lock (gate)
        {
            ended = true;
            drain = activePosts == 0 ? Task.CompletedTask : postsDrained!.Task;
        }
        await drain.ConfigureAwait(false);
        lock (gate)
        {
            failure ??= postFailure;
            if (failure is null && posted != ReplyCount)
                failure = new InvalidOperationException(AllocatorQueryMailboxNames.MissingReplies);

            producerFailure = failure;
        }
        if (failure is not null)
        {
            try
            {
                await cancellation.CancelAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                failure.Data[AllocatorQueryMailboxNames.CleanupFailure] = cleanup;
            }
            completed.TrySetException(failure);
        }
        else
            completed.TrySetResult();
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        lock (gate)
        {
            if (disposed is not null)
                return new(disposed.Task);

            completion = disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            closing = true;
        }
        _ = DisposeCore(completion);

        return new(completion.Task);
    }

    private async Task DisposeCore(TaskCompletionSource completion)
    {
        Exception? failure = null;
        try
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        bool join;
        lock (gate)
            join = started;
        if (join)
        {
            try
            {
                await completed.Task.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Producer failure belongs to Completion; disposal still joins it.
            }
        }
        else
            completed.TrySetCanceled(cancellation.Token);
        cancellation.Dispose();
        if (failure is null)
            completion.TrySetResult();
        else
            completion.TrySetException(failure);
    }
}
