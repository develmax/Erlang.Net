namespace Erlang.Tests;

internal sealed class NifLifetimeTestOwner(Func<ValueTask>? cleanup = null) : IAsyncDisposable
{
    internal int Calls;

    public async ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref Calls);
        if (cleanup is not null)
            await cleanup();
    }
}
