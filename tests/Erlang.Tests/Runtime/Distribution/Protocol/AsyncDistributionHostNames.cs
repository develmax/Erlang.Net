namespace Erlang.Tests;

internal static class AsyncDistributionHostNames
{
    internal const string Test = "host_async_distribution_connected_staging_and_hard_bound";
    internal const string Failure = "Async distribution staging assertion failed";
    internal const string ProcessFlag = "process_flag";
    internal const string AsyncDistribution = "async_dist";
    internal const string TrapExit = "trap_exit";
    internal const string SaveCalls = "save_calls";
    internal const string Send = "send";
    internal const string NoSuspend = "send_nosuspend";
    internal const string NoConnect = "noconnect";
    internal const string Normal = "async_distribution_normal";
    internal const string First = "async_distribution_first";
    internal const string Second = "async_distribution_second";
    internal const string Kill = "kill";
    internal const string Invalid = "invalid_async_distribution_value";
    internal const string Gate = "gate";
    internal const string AccountedBytes = "AccountedBytes";
    internal const string Statistics = "dist_get_stat";
    internal const string Drain = "DrainOwnedOutputOperations";
    internal const string ClosureModule = "async_distribution_closure";
    internal const string ClosureFunction = "make";
    internal const string ClosureSource = "-module(async_distribution_closure). -export([make/0]). make() -> fun() -> ok end.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(8);
}
