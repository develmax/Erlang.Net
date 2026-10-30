namespace Erlang;

internal sealed partial class RuntimeStatistics
{
    private ulong completedVmCollections;
    private ulong reclaimedVmWords;

    // Called only after the real owned collector has committed. Count physical
    // allocation words, not child payload slots, CLR bytes or finalizer work.
    internal void RecordVmCollection(long reclaimedAllocationWords)
    {
        lock (gate)
        {
            completedVmCollections = unchecked(completedVmCollections + 1);
            reclaimedVmWords = unchecked(reclaimedVmWords + (ulong)reclaimedAllocationWords);
        }
    }

    // Internal until ordinary/public collector adoption is complete. This
    // method must not silently turn unobserved CLR collection into VM events.
    internal Term SampleVmCollections()
    {
        ulong collections;
        ulong words;
        lock (gate)
        {
            collections = completedVmCollections;
            words = reclaimedVmWords;
        }

        return Term.Tuple(new Integer(collections), new Integer(words), Term.I(RuntimeStatisticsParameters.GarbageCollectionReserved));
    }
}
