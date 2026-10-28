namespace Erlang.Tests;

internal static class NativeResourceAllocation570Names
{
    internal const string Test = "runtime/resource570/native-bytes-and-owned-cell-domains";
    internal const string Heap = "TermHeap506";
    internal const string Allocation = "AllocationSnapshot567";
    internal const string Nursery = "NurseryAllocationWords";
    internal const string Survivor = "SurvivorAllocationWords";
    internal const string Old = "OldAllocationWords";
    internal const string Reclaimed = "ReclaimedAllocationWords";
    internal const string DomainSnapshot = "Snapshot524";
    internal const string NativeBytes = "LiveNativeBytes";
    internal const string NativeCount = "LiveResources";
    internal const string NativeReclaimed = "Reclaimed";
    internal const string AllocationBytes = "AllocationBytes";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string ReleaseNative = "ReleaseNative524";
    internal const string Collect = "Collect";
    internal const string Retired = "Retired524";
    internal const string RetireImage = "RetireAsync";
    // Actual tuple allocation: one header and its two Term cells.
    internal const int TupleAllocationWords = 3;
}
