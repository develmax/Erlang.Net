namespace Erlang;

internal sealed partial class RuntimeEtsReadLease : IDisposable
{
    private readonly object gate = new();
    private RuntimeEtsStoredObject? owner;

    internal RuntimeEtsReadLease(RuntimeEtsStoredObject owner) => this.owner = owner;

    internal Term Materialize(CancellationToken cancellation)
    {
        lock (gate)
        {
            var current = owner ?? throw new ObjectDisposedException(nameof(RuntimeEtsReadLease));

            return current.Materialize(cancellation);
        }
    }

    public void Dispose()
    {
        RuntimeEtsStoredObject? retired;
        lock (gate)
        {
            retired = owner;
            owner = null;
        }
        retired?.Release();
    }
}
