namespace Erlang;

internal sealed class NativeResourceRootSet524(IDisposable[] leases) : IDisposable
{
    private IDisposable[]? owned = leases;
    private readonly List<Exception> failures = new(leases.Length);

    public void Dispose() => DisposeCore593(false);

    internal void DisposeGuestGc593() => DisposeCore593(true);

    private void DisposeCore593(bool guestGc)
    {
        IDisposable[]? retired = Interlocked.Exchange(ref owned, null);
        if (retired is null) return;
        foreach (IDisposable lease in retired)
            try
            {
                if (guestGc) NativeResourceRoots524.DisposeGuestGraph593(lease);
                else lease.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
