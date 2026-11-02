namespace Erlang;

internal sealed class NativeResourceRootSet524(IDisposable[] leases) : IDisposable
{
    private IDisposable[]? owned = leases;
    private readonly List<Exception> failures = new(leases.Length);

    // Only called while a real mailbox operation protects this set's owner.
    internal PreparedLoaderLease807? FindPreparedGuest807(PreparedLoaderOwner807 owner)
    {
        foreach (IDisposable leaf in Volatile.Read(ref owned) ?? [])
        {
            if (leaf is PreparedLoaderLease807 prepared
                && prepared.Kind807 == PreparedLoaderLeaseKind807.Guest && prepared.Owns807(owner)) return prepared;
            if (leaf is NativeResourceRootSet524 nested && nested.FindPreparedGuest807(owner) is { } found) return found;
        }

        return null;
    }

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
