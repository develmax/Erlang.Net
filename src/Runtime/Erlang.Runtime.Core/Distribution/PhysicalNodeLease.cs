namespace Erlang;

internal sealed class PhysicalNodeLease : IDisposable
{
    private RuntimeKnownNodes? owner;
    internal PhysicalNodeEntry Entry { get; }
    internal nint Address => Read().Address;

    internal PhysicalNodeLease(RuntimeKnownNodes owner, PhysicalNodeEntry entry)
    {
        this.owner = owner;
        Entry = entry;
    }

    internal PhysicalNodeEntry Read()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref owner) is null, this);

        return Entry;
    }

    internal LocalNodeSnapshot Snapshot => Read().Read();

    internal PhysicalNodeLease Retain() => (Volatile.Read(ref owner) ?? throw new ObjectDisposedException(nameof(PhysicalNodeLease))).RetainPhysicalNode(this);

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.ReleasePhysicalNode(Entry);
}
