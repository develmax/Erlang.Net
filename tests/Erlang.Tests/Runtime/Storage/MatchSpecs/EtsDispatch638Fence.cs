namespace Erlang.Tests;

// Captures actual Task.Yield continuations from the registered public body.
// It supplies no production callback, native hold or synthetic lifetime flag.
internal sealed class EtsDispatch638Fence : SynchronizationContext, IDisposable
{
    private readonly object gate = new();
    private readonly Queue<(SendOrPostCallback Callback, object? State)> pending = new();
    private readonly TaskCompletionSource posted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool released;

    internal Task Posted => posted.Task;

    public override void Post(SendOrPostCallback callback, object? state)
    {
        lock (gate)
        {
            if (!released)
            {
                pending.Enqueue((callback, state));
                posted.TrySetResult();

                return;
            }
        }
        Queue(callback, state);
    }

    internal void Release()
    {
        lock (gate)
            released = true;
        while (true)
        {
            (SendOrPostCallback Callback, object? State) item;
            lock (gate)
            {
                if (!pending.TryDequeue(out item))
                    return;
            }
            Queue(item.Callback, item.State);
        }
    }

    private static void Queue(SendOrPostCallback callback, object? state)
        => ThreadPool.UnsafeQueueUserWorkItem(static item => item.Callback(item.State),
            (Callback: callback, State: state), preferLocal: false);

    public void Dispose() => Release();
}
