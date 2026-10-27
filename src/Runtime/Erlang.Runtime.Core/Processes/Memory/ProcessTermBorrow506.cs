namespace Erlang;

internal sealed class ProcessTermBorrow506(ProcessTermHeap506 owner, ProcessTermFrame506 frame, Term value) : IDisposable
{
    private int disposed;
    internal ProcessTermHeap506 Owner { get; } = owner;
    internal Term Value { get; } = value;
    internal bool IsDisposed => Volatile.Read(ref disposed) != 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
            frame.Dispose();
    }
}
