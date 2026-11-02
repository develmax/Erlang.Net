namespace Erlang;

internal sealed class PreparedLoaderLease807 : IDisposable
{
    private PreparedLoaderOwner807? owner;
    private readonly PreparedLoaderLeaseKind807 kind;

    internal PreparedLoaderLease807(PreparedLoaderOwner807 owner, PreparedLoaderLeaseKind807 kind)
    {
        this.owner = owner;
        this.kind = kind;
    }

    internal PreparedLoaderLeaseKind807 Kind807 => kind;

    internal bool Owns807(PreparedLoaderOwner807 expected) => ReferenceEquals(Volatile.Read(ref owner), expected);

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.Release807(kind);
}
