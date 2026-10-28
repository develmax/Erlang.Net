namespace Erlang;

internal sealed class NativeResourceRootLease524(NativeResourcePayload524 owner) : IDisposable
{
    private int admitted;

    internal void Admit524() => Volatile.Write(ref admitted, 1);

    internal void DisposeGuestGc536()
    {
        if (Interlocked.CompareExchange(ref admitted, 2, 1) == 1)
            owner.ReleaseGuestGcRoot536();
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref admitted, 2, 1) == 1)
            owner.ReleaseRoot524();
    }
}
