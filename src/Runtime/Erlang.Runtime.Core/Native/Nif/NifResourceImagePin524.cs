namespace Erlang;

internal sealed class NifResourceImagePin524(NifImageBindings owner) : IDisposable
{
    private int admitted;

    internal void Admit524() => Volatile.Write(ref admitted, 1);

    internal bool IsActiveFor524(NifImageBindings candidate)
        => ReferenceEquals(owner, candidate) && Volatile.Read(ref admitted) == 1;

    internal NifResourceImagePin524 Fork524() => owner.RetainResourceImage524(this);

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref admitted, 2, 1) == 1)
            owner.ReleaseCall();
    }
}
