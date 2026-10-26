namespace Erlang;

internal sealed partial class RuntimeStatistics
{
    private readonly long initialAllocated457 = GC.GetTotalAllocatedBytes(precise: true);
    private readonly long initialHeap457 = GC.GetTotalMemory(forceFullCollection: false);
    private readonly int initialCollections457 = GC.CollectionCount(0);
    private long observedReclaimedBytes457;
    private int observedCollections457;

    // The managed VM reports its real CLR collector, just as runtime/user CPU
    // reports its actual host process. CLR heap readings are estimates and are
    // not an ERTS per-process heap or native-word accounting compatibility claim.
    // GetTotalMemory does not force a collection. Only observed positive net
    // reclamation advances the cumulative estimate; no allocation is triggered
    // to manufacture a nonzero result or influence the guest's challenge.
    private Term SampleManagedGarbage457()
    {
        long heap = GC.GetTotalMemory(forceFullCollection: false);
        long allocated = GC.GetTotalAllocatedBytes(precise: true);
        long reclaimed = allocated - initialAllocated457 - (heap - initialHeap457);
        int collections = Math.Max(0, GC.CollectionCount(0) - initialCollections457);
        if (collections > observedCollections457)
        {
            observedReclaimedBytes457 = Math.Max(observedReclaimedBytes457, Math.Max(0, reclaimed));
            observedCollections457 = collections;
        }

        return Term.Tuple(Term.I(collections), Term.I(observedReclaimedBytes457 / IntPtr.Size), Term.I(0));
    }
}
