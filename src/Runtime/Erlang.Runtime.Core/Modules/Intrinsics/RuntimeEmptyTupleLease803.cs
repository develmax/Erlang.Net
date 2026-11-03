namespace Erlang;

internal sealed class RuntimeEmptyTupleLease803(RuntimeEmptyTupleDomain803 domain) : IDisposable
{
    private int disposed;
    internal bool IsDisposed => Volatile.Read(ref disposed) != 0;
    internal RuntimeEmptyTupleDomain803 Domain => domain;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0) domain.Release803(this);
    }
}
