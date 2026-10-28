namespace Erlang;

internal sealed class ProcessNativePublication524(
    ProcessTermHeap506 owner,
    ProcessNativeHeader524[] inserted
) : IDisposable
{
    private bool committed;

    internal void Commit524() => committed = true;

    public void Dispose()
    {
        if (committed) return;
        committed = true;
        owner.RollbackNativeHeaders524(inserted);
    }
}
