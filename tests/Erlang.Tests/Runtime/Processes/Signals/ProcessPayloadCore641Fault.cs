namespace Erlang.Tests;

// This is a real IAsyncDisposable attached to the actual Work641. It injects
// cleanup failure through the real release worker, not a fabricated failure list.
internal sealed class ProcessPayloadCore641Fault(Exception failure, AsyncLocal<object?> ambient) : IAsyncDisposable
{
    private int disposals;
    internal int Disposals => Volatile.Read(ref disposals);
    internal bool DefaultContext { get; private set; }

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref disposals);
        DefaultContext = ambient.Value is null && SynchronizationContext.Current is null;

        return ValueTask.FromException(failure);
    }
}
