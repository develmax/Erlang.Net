namespace Erlang.Compiler;

internal sealed class OwnedTrampolineCheckpointScope506(Action restore) : IDisposable
{
    private int disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
            restore();
    }
}
