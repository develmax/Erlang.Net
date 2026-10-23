namespace Erlang;

public sealed class ReferenceResourceLease<T> : IDisposable where T : class
{
    private ReferenceResourceEntry? entry;

    internal ReferenceResourceLease(ReferenceResourceEntry entry) => this.entry = entry;

    public T Value
    {
        get
        {
            ReferenceResourceEntry? current = Volatile.Read(ref entry);
            ObjectDisposedException.ThrowIf(current is null, ReferenceResourceNames.Lease);

            return current.Read<T>();
        }
    }

    public void Dispose() => Interlocked.Exchange(ref entry, null);
}
