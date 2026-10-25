namespace Erlang;

// Native TLS is scoped on the calling physical thread. It deliberately does
// not use AsyncLocal: another CLR continuation is not a driver-created thread.
internal sealed class ErlDriverOwnerScope420 : IDisposable
{
    private readonly ErlDriverThreadBridge420 bridge;
    private readonly int thread;
    private readonly ulong previous;
    private bool disposed;

    internal ErlDriverOwnerScope420(ErlDriverThreadBridge420 bridge, ulong owner)
    {
        this.bridge = bridge;
        thread = Environment.CurrentManagedThreadId;
        previous = bridge.ExchangeOwner(owner);
    }

    public void Dispose()
    {
        if (disposed) return;
        if (thread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException(ErlDriverThreadBridgeNames420.WrongThread);
        bridge.ExchangeOwner(previous);
        disposed = true;
    }
}
