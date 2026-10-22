namespace Erlang;

// A port, outstanding native invocation, selected event or asynchronous worker
// must retain a distinct lease until its final native callback has returned.
internal sealed class ErlDriverImageLease420 : IDisposable
{
    private ErlDriverImage420? owner;
    private readonly bool worker;

    internal ErlDriverImageLease420(ErlDriverImage420 owner, bool worker = false)
    {
        this.owner = owner;
        this.worker = worker;
    }

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.Release(worker);
}
