namespace Erlang;

internal sealed class NativeResourceTypeLease524(NativeResourceType524 owner) : IDisposable
{
    private int admitted;

    internal void Admit524() => Volatile.Write(ref admitted, 1);

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref admitted, 2, 1) == 1)
            owner.ReleasePayload524();
    }
}
