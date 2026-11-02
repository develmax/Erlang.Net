namespace Erlang;

internal sealed class ModuleLiteralLease800(ModuleLiteralDomain800 domain) : IDisposable
{
    private int disposed;

    internal ModuleLiteralDomain800 Domain => domain;
    internal bool IsDisposed => Volatile.Read(ref disposed) != 0;

    internal bool TryFork(Term root, out ModuleLiteralLease800? lease)
        => domain.TryFork800(this, root, out lease);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
            domain.Release800(this);
    }
}
