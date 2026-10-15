namespace Erlang.Tests;

internal sealed class NifSourceEntryTestOwner : IAsyncDisposable
{
    internal int Calls;

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref Calls);

        return ValueTask.CompletedTask;
    }
}
