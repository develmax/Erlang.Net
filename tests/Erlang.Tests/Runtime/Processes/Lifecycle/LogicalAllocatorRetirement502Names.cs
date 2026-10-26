namespace Erlang.Tests;

internal static class LogicalAllocatorRetirement502Names
{
    internal const string Test = "runtime/allocator502/logical-exit-before-actual-auxiliary-drain";
    internal const string Registered = "logical_allocator502_target";
    internal const string Dispatcher = "actorDispatcher";
    internal const string PrepareAuxiliary = "PrepareAuxiliary420";
    internal const string AssignedLane = "AssignedLane420";
    internal const string Publish = "Publish";
    internal const string Completion = "Completion";
    internal const string Owner = "allocatorGather434";
    internal const string Pending = "pending";
    internal const string Origin = "Origin";
    internal const string PhysicalDone = "PhysicalDone";
    internal const string TaskProperty = "Task";
    internal const string Batch = "Batch";
    internal const string RuntimeGate = "gate";
    internal const string Drained = "Drained";
    internal const string Failure = "Actual allocator auxiliary ownership502 must outlive logical process retirement";
    internal const int HistogramWidth = 5;
    internal const int HistogramStart = 8;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
