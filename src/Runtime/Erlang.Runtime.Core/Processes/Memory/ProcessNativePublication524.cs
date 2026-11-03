namespace Erlang;

internal sealed class ProcessNativePublication524 : IDisposable
{
    private readonly ProcessTermHeap506 owner;
    private readonly ProcessNativeHeader524[] inserted;
    private readonly NativeOffheapHeader576[]? promoted;
    private readonly ResourceBinaryDescriptor577[]? binaryPromoted;
    private readonly ManagedFunctionDispatchEntry[]? functionPromoted;
    private bool committed;

    internal ProcessNativePublication524(
        ProcessTermHeap506 owner,
        ProcessNativeHeader524[] inserted,
        NativeOffheapHeader576[]? promoted = null,
        ResourceBinaryDescriptor577[]? binaryPromoted = null
    )
        : this(
            owner,
            inserted,
            promoted,
            binaryPromoted,
            null
        )
    {
    }

    internal ProcessNativePublication524(
        ProcessTermHeap506 owner,
        ProcessNativeHeader524[] inserted,
        NativeOffheapHeader576[]? promoted,
        ResourceBinaryDescriptor577[]? binaryPromoted,
        ManagedFunctionDispatchEntry[]? functionPromoted
    )
    {
        this.owner = owner;
        this.inserted = inserted;
        this.promoted = promoted;
        this.binaryPromoted = binaryPromoted;
        this.functionPromoted = functionPromoted;
    }

    internal void Commit524()
    {
        if (promoted is not null) foreach (var header in promoted) header.Published = true;
        if (binaryPromoted is not null) foreach (var descriptor in binaryPromoted) descriptor.Published = true;
        if (functionPromoted is not null) foreach (var entry in functionPromoted) entry.Published = true;
        committed = true;
    }

    public void Dispose()
    {
        if (committed) return;
        committed = true;
        owner.RollbackNativeHeaders524(inserted);
    }
}
