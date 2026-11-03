namespace Erlang.Tests;

internal sealed class Nonblocking641Probe(object gate, AsyncLocal<object?> ambient, Exception? failure) : IAsyncDisposable
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int disposals;
    internal Task Started => started.Task;
    internal int Disposals => Volatile.Read(ref disposals);
    internal bool DefaultContext { get; private set; }
    internal bool OutsideCallerGate { get; private set; }

    internal void Release() => release.TrySetResult();

    public async ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref disposals);
        DefaultContext = ambient.Value is null && SynchronizationContext.Current is null;
        OutsideCallerGate = !Monitor.IsEntered(gate);
        lock (gate) { }
        started.TrySetResult();
        await release.Task.ConfigureAwait(false);
        if (failure is not null) throw failure;
    }
}
