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

    internal Term MaterializeSelected594(CancellationToken cancellation, AtomicsHostCopyTransaction591 transaction)
    {
        lock (gate)
        {
            var current = owner ?? throw new ObjectDisposedException(nameof(RuntimeEtsReadLease));

            return current.MaterializeSelected594(cancellation, transaction);
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
