namespace Erlang;

public sealed class ReferenceResourceLease<T> : IDisposable where T : class
{
    private ReferenceResourceEntry? entry;
    private readonly PreparedLoaderOwner807? preparedOwner807;
    private PreparedLoaderHost807? preparedHost807;

    internal ReferenceResourceLease(ReferenceResourceEntry entry) => this.entry = entry;

    internal ReferenceResourceLease(ReferenceResourceEntry entry, PreparedLoaderOwner807 owner, PreparedLoaderHost807 host)
    {
        this.entry = entry;
        preparedOwner807 = owner;
        preparedHost807 = host;
    }

    public T Value
    {
        get
        {
            ReferenceResourceEntry? current = Volatile.Read(ref entry);
            ObjectDisposedException.ThrowIf(current is null, ReferenceResourceNames.Lease);

            if (preparedOwner807 is { } owner)
            {
                PreparedLoaderHost807 host = Volatile.Read(ref preparedHost807)
                    ?? throw new ObjectDisposedException(ReferenceResourceNames.Lease);
                PreparedBeamImage image = host.ReadImage807(owner);
                if (image.IsConsumed) throw new InvalidOperationException(ReferenceResourceNames.ConsumedPrepared);

                return (T)(object)image;
            }

            return current.Read<T>();
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref entry, null);
        Interlocked.Exchange(ref preparedHost807, null)?.Dispose();
    }
}
