namespace Erlang;

internal sealed class ProcessNativePublication524(
    ProcessTermHeap506 owner,
    ProcessNativeHeader524[] inserted,
    NativeOffheapHeader576[]? promoted = null,
    ResourceBinaryDescriptor577[]? binaryPromoted = null
) : IDisposable
{
    private bool committed;

    internal void Commit524()
    {
        if (promoted is not null)
            foreach (NativeOffheapHeader576 header in promoted)
                header.Published = true;
        if (binaryPromoted is not null)
            foreach (ResourceBinaryDescriptor577 descriptor in binaryPromoted)
                descriptor.Published = true;
        committed = true;
    }

    public void Dispose()
    {
        if (committed) return;
        committed = true;
        owner.RollbackNativeHeaders524(inserted);
    }
}
