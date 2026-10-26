namespace Erlang;

internal sealed class ProcessWordOperation504(ProcessWordExecution504 owner) : IDisposable
{
    private int disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
            owner.Leave();
    }
}
