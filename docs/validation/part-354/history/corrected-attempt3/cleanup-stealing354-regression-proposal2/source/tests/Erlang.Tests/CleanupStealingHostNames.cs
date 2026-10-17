namespace Erlang.Tests;

internal static class CleanupStealingHostNames
{
    internal const string Test = "runtime/canceled-unbound-continuation-stolen-before-bound-blocker-release";
    internal const string Module = "erlang";
    internal const string ProcessFlag = "process_flag";
    internal const string Scheduler = "scheduler";
    internal const string SystemInfo = "system_info";
    internal const string SchedulerId = "scheduler_id";
    internal const string Failure = "Canceled unbound actor continuation failed cleanup-stealing contract";
    internal const string BlockerEntered = "CLEANUP_BLOCKER_ENTERED";
    internal const string ObserverEntered = "CLEANUP_OBSERVER_ENTERED_BEFORE_RELEASE";
    internal const int Lanes = 2;
    internal const int BoundLane = 1;
    internal const int Unbound = 0;
    internal const int HaltStatus = 41;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
