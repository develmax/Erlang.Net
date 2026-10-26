namespace Erlang;

internal sealed class ErlDriverPortReadLease420 : IDisposable
{
    private ErlDriverPort420? port;

    internal ErlDriverPortReadLease420(ErlDriverPort420 port) => this.port = port;

    internal ErlDriverPort420 Port => port ?? throw new ObjectDisposedException(nameof(ErlDriverPortReadLease420));

    public void Dispose() => Interlocked.Exchange(ref port, null)?.ReleaseService();
}
