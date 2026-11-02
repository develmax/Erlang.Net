namespace Erlang;

internal sealed class PreparedLoaderImageReservations807(int capacity) : IDisposable
{
    private PreparedLoaderReservation807?[]? held = new PreparedLoaderReservation807[capacity];
    private readonly List<Exception> failures = new(capacity);

    internal void Set807(int index, PreparedLoaderReservation807 reservation)
        => held![index] = reservation;

    public void Dispose()
    {
        PreparedLoaderReservation807?[]? selected = Interlocked.Exchange(ref held, null);
        if (selected is null) return;
        foreach (PreparedLoaderReservation807? reservation in selected)
            try
            {
                reservation?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
