namespace Erlang;

internal sealed partial class NativeResourceDomain524
{
    internal bool PhysicallyRetired528
    {
        get
        {
            lock (gate)
                return workerJoined && live.Count == 0 && head is null && types.Count == 0
                    && dispatchFailure is null && pendingLoads527 == 0;
        }
    }
}
