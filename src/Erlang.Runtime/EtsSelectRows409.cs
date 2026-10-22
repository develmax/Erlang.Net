namespace Erlang;

internal static class EtsSelectRows409
{
    internal static TupleTerm[] Order(TupleTerm[] rows, long keyPosition, CancellationToken cancellation)
    {
        var scratch = new TupleTerm[rows.Length];
        for (long width = 1; width < rows.Length; width *= 2)
        {
            for (long start = 0; start < rows.Length; start += width * 2)
            {
                int left = (int)start;
                int middle = (int)Math.Min(start + width, rows.Length);
                int right = middle;
                int end = (int)Math.Min(start + width * 2, rows.Length);
                for (int output = left; output < end; output++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    bool takeLeft = left < middle && (right >= end || TermOrder.Compare(
                        EtsCorePlanner407.Key(rows[left], keyPosition),
                        EtsCorePlanner407.Key(rows[right], keyPosition)
                    ) <= 0);
                    scratch[output] = rows[takeLeft ? left++ : right++];
                }
            }
            (rows, scratch) = (scratch, rows);
        }

        return rows;
    }

    internal static TupleTerm[] Materialize(RuntimeEtsReadLease?[] leases, CancellationToken cancellation)
    {
        var result = new TupleTerm[leases.Length];
        for (int index = 0; index < leases.Length; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            result[index] = (TupleTerm)leases[index]!.Materialize(cancellation);
        }

        return result;
    }

    internal static void Release(RuntimeEtsReadLease?[] leases, Exception? original)
    {
        List<Exception>? failures = null;
        foreach (var lease in leases)
        {
            try
            {
                lease?.Dispose();
            }
            catch (Exception cleanup)
            {
                failures ??= original is null ? [] : [original];
                failures.Add(cleanup);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
