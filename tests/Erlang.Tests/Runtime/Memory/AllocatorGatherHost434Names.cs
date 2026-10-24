namespace Erlang.Tests;

internal static class AllocatorGatherHost434Names
{
    internal const string Module = "erts_internal", Hist = "gather_alloc_histograms", Carriers = "gather_carrier_info";
    internal const string Failure = "Actual allocator owner window/physical request ownership differs";
    internal const string Kill = "kill";
    internal const string Runtime = "erlang", ProcessFlag = "process_flag", Scheduler = "scheduler";
    internal const int CallerLane = 2, HeldLane = 1;
    internal const string Nif = "Erlang.NifNativeAllocationOwner", NifModule = "actual_allocator434";
    internal const string Driver = "driver_alloc", Standard = "std_alloc", Literal = "literal_alloc", LongLived = "ll_alloc";
    internal const string HistTest = "runtime/allocator434/actual-nif-span-histogram-and-realloc";
    internal const string ArrayTest = "runtime/allocator434/actual-array-and-persistent-owned-carriers";
    internal const string RetirementTest = "runtime/allocator434/actual-aux-request-kill-physical-join";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
