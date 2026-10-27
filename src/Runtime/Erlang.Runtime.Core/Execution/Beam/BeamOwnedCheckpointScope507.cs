namespace Erlang;

internal sealed class BeamOwnedCheckpointScope507(Action restore) : IDisposable
{
    private int disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
            restore();
    }
}
