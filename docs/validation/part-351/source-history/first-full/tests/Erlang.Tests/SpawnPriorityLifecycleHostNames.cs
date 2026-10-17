namespace Erlang.Tests;

internal static class SpawnPriorityLifecycleHostNames
{
    internal const string Module = "priority_lifecycle_host_probe";
    internal const string Bridge = "bridge";
    internal const string Target = "target";
    internal const string Low = "low";
    internal const string Normal = "normal";
    internal const string High = "high";
    internal const string Priority = "priority";
    internal const string Started = "H-start";
    internal const string Ended = "H-end";
    internal const string Send = "send";
    internal const string Release = "release";
    internal const string HighWaitOrder = "H-start,N,H-end";
    internal const string Kill = "kill";
    internal const string Promoted = "promoted";
    internal const string Record = "record";
    internal const string ProcessInfo = "process_info";
    internal const string ProcessesField = "processes";
    internal const string GateField = "gate";
    internal const string ChangePriorityMethod = "ChangeSchedulingPriority";
    internal const string Delimiter = ",";
    internal const int QueuedChangeChildren = 2;
    internal const int SuccessfulHaltStatus = 0;
    internal const string PromotedOrder = "promoted,normal";
    internal const string DispatcherField = "actorDispatcher";
    internal const string CompletionProperty = "Completion";
    internal const string Bump = "bump_reductions";
    internal const string Erlang = "erlang";
    internal const string Spawn = "spawn_opt";
    internal const string Done = "done";
    internal const string LowMark = "L";
    internal const string NormalMark = "N";
    internal const string Failure = "Priority ownership/epoch lifecycle control failed.";
    internal const int TimeoutSeconds = 10;
    internal const int LogicalSlices = 12;
    internal const int MaximumBump = 4000;
    internal const int ExpectedNormalBeforeLow = 7;
}
