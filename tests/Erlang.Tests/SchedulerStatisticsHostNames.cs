namespace Erlang.Tests;

internal static class SchedulerStatisticsHostNames
{
    internal const string Test = "runtime/scheduler-statistics-queued-owner-running-idle-and-offline-lanes";
    internal const string Failure = "Scheduler queue statistics do not match actual admitted guest owners";
    internal const string Erlang = "erlang";
    internal const string Flag = "process_flag";
    internal const string SystemFlag = "system_flag";
    internal const string Scheduler = "scheduler";
    internal const string Online = "schedulers_online";
    internal const string Spawn = "spawn_opt";
    internal const string Monitor = "monitor";
    internal const string Down = "DOWN";
    internal const string Normal = "normal";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
