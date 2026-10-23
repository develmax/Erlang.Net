namespace Erlang;

internal sealed class PersistentArenaRoots416(PersistentArenaRoot416[] roots) : IDisposable
{
    private PersistentArenaRoot416[]? owned = roots;
    // Failure storage is staged before any native allocation/ownership transfer.
    private readonly List<Exception> failures = new(roots.Length);
    internal int Count => (Volatile.Read(ref owned) ?? throw new ObjectDisposedException(PersistentArenaNames416.Closed)).Length;

    internal PersistentArenaRoot416 Get(int index)
    {
        var current = Volatile.Read(ref owned) ?? throw new ObjectDisposedException(PersistentArenaNames416.Closed);
        if ((uint)index >= (uint)current.Length) throw new InvalidDataException(PersistentArenaNames416.Invalid);

        return current[index];
    }

    public void Dispose()
    {
        var retired = Interlocked.Exchange(ref owned, null);
        if (retired is null) return;
        // Genuine epoch leases require deterministic release, O(offheap roots).
        // A failed lease cannot starve later releases. SafeHandle records failure.
        foreach (var root in retired)
            try
            {
                root.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
