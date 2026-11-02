namespace Erlang.Tests;

internal sealed class NativeResource524ImageOwner : IAsyncDisposable
{
    private int disposed;
    internal bool Disposed => Volatile.Read(ref disposed) != 0;

    public ValueTask DisposeAsync()
    {
        Volatile.Write(ref disposed, 1);

        return ValueTask.CompletedTask;
    }
}
