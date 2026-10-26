namespace Erlang;

internal sealed class RuntimeMapHashNodeLease : IDisposable
{
    private readonly WeakReference<RuntimeKnownNodes> owner;
    private RuntimeMapHashEpoch? entry;
    internal ulong Word { get; }

    internal RuntimeMapHashNodeLease(ulong word, RuntimeKnownNodes owner, RuntimeMapHashEpoch entry)
    {
        Word = word;
        this.owner = new(owner);
        this.entry = entry;
    }

    public void Dispose()
    {
        RuntimeMapHashEpoch? release = Interlocked.Exchange(ref entry, null);
        if (release is not null && owner.TryGetTarget(out var live)) live.ReleaseMapHashNode(release);
    }
}
