namespace Erlang;

internal sealed class RuntimeProcessPayloadWork641 : IThreadPoolWorkItem
{
    private readonly RuntimeProcessPayloadOwner641 owner;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource done = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IAsyncDisposable? payload;
    private Task? dependency;
    private int ended, scheduled, queueSettled, executing;
    private Exception? schedulingFailure;
    internal RuntimeProcessPayloadWork641? NextFailure641;
    internal ProcessContext? Context641 { get; }
    internal Task Completion641 => done.Task;
    internal Exception? Failure641 { get; private set; }
    internal RuntimeProcessPayloadOwner641 Owner641 => owner;

    internal RuntimeProcessPayloadWork641(RuntimeProcessPayloadOwner641 owner, ProcessContext? context)
    {
        this.owner = owner;
        Context641 = context;
    }

    internal void Attach641(IAsyncDisposable value)
    {
        if (Interlocked.CompareExchange(ref payload, value, null) is not null || Volatile.Read(ref ended) != 0)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
    }

    internal void EndAfter641(Task task)
    {
        if (Volatile.Read(ref ended) != 0 || Interlocked.CompareExchange(ref dependency, task, null) is not null)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        End641();
    }

    internal void End641()
    {
        if (Interlocked.Exchange(ref ended, 1) != 0) return;
        Volatile.Write(ref scheduled, 1);
        try
        {
            if (!ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false))
                throw new InvalidOperationException(RuntimeProcessPayloadNames641.Queue);
        }
        catch (Exception error)
        {
            Interlocked.CompareExchange(ref schedulingFailure, error, null);
        }
        finally
        {
            Volatile.Write(ref queueSettled, 1);
            ready.TrySetResult();
        }
    }

    internal async Task Join641()
    {
        await ready.Task.ConfigureAwait(false);
        Execute(); // Actual ready-work fallback if its enqueue failed.
        await done.Task.ConfigureAwait(false);
    }

    private static Exception? Combine641(Exception? first, Exception? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        try
        {
            return new AggregateException(first, second);
        }
        catch
        {
            return first;
        } // Root cleanup/completion still must be published.
    }

    public void Execute()
    {
        if (Volatile.Read(ref scheduled) == 0) return;
        var spin = new SpinWait();
        while (Volatile.Read(ref queueSettled) == 0) spin.SpinOnce();
        if (Interlocked.CompareExchange(ref executing, 1, 0) != 0) return;
        using var atoms = new AtomNameScope(owner.Runtime641.AtomNames);
        Exception? waited = null, released = null;
        try
        {
            Volatile.Read(ref dependency)?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (Volatile.Read(ref dependency)?.IsCanceled == true) { }
        catch (Exception error)
        {
            waited = error;
        }
        try
        {
            Interlocked.Exchange(ref payload, null)?.DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            released = error;
        }
        Exception? failure = Volatile.Read(ref schedulingFailure);
        failure = Combine641(failure, waited);
        failure = Combine641(failure, released);
        Failure641 = failure;
        if (failure is null) done.TrySetResult(); else done.TrySetException(failure);
        owner.Finished641(this, failure);
    }
}
