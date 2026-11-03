namespace Erlang;

// Retained internal HOST diagnostics retain a real table/pair read lease.
internal sealed class PersistentTermArenaSnapshot417(PersistentTableRead416 read) : IDisposable
{
    private PersistentTableRead416? owned = read;

    internal TupleTerm?[] Materialize(CancellationToken cancellation)
    {
        var current = Volatile.Read(ref owned) ?? throw new ObjectDisposedException(PersistentTermNativeNames.Closed);

        return AtomicsHostAggregate594.Publish(transaction =>
            current.PairsSelected594(cancellation, transaction).Cast<TupleTerm?>().ToArray());
    }

    public void Dispose() => Interlocked.Exchange(ref owned, null)?.Dispose();
}
