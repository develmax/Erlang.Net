namespace Erlang;

internal sealed class PersistentTermReadSnapshot(RuntimeEtsReadLease?[] leases) : IDisposable
{
    private RuntimeEtsReadLease?[]? owned = leases;

    internal TupleTerm?[] Materialize(CancellationToken cancellation)
    {
        var current = owned ?? throw new ObjectDisposedException(nameof(PersistentTermReadSnapshot));
        var pairs = new TupleTerm?[current.Length];
        for (int index = 0; index < current.Length; index++)
            if (current[index] is { } lease)
                pairs[index] = (TupleTerm)lease.Materialize(cancellation);

        return pairs;
    }

    public void Dispose()
    {
        var retired = Interlocked.Exchange(ref owned, null);
        if (retired is null)
            return;
        var failures = new List<Exception>();
        foreach (var lease in retired)
        {
            try
            {
                lease?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(failures);
    }
}
