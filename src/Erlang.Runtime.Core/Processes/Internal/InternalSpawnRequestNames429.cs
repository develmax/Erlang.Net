namespace Erlang;

internal static class InternalSpawnRequestNames429
{
    internal const string Module = "erts_internal", Function = "spawn_request";
    internal const string MinimumHeap = "min_heap_size", MinimumBinaryHeap = "min_bin_vheap_size", MaximumHeap = "max_heap_size";
    internal const string HeapPartition = "Internal spawn request429 native heap-allocation options require an implemented process heap policy.";
    internal const string BadArgument = "badarg", BadOptions = "badopt";
    internal const string Foreign = "Internal spawn request429 requires its actual live runtime actor.";
}
