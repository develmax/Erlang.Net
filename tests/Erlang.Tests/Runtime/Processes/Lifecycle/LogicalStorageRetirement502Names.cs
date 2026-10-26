namespace Erlang.Tests;

internal static class LogicalStorageRetirement502Names
{
    internal const string Test = "runtime/process-liveness502/actual-ets-retirement-before-logical-exit";
    internal const string Registered = "logical_storage502_target";
    internal const string LeaseOwner = "owner";
    internal const string Gate = "gate";
    internal const string ExitContinuations = "exitContinuations";
    internal const string StorageRetirement = "StorageRetirement";
    internal const string AliveRequests = "processAliveRequests425";
    internal const string Count = "Count";
    internal const string Failure = "Actual ETS logical retirement502 must precede liveness replies and DOWN";
    internal const int AdmittedQueries = 2;
    internal const int StorageLane = 1;
    internal const int QueryLane = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(1);
}
