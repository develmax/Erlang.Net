namespace Erlang.Tests;

internal static class SpawnAsyncDistributionHostNames
{
    internal const string Test = "host_spawn_async_dist_first_instruction_validation_and_dead_parent";
    internal const string Failure = "Spawn async distribution assertion failed";
    internal const string Spawn = "spawn_opt";
    internal const string Request = "spawn_request";
    internal const string Flag = "async_dist";
    internal const string Information = "process_info";
    internal const string ProcessFlag = "process_flag";
    internal const string Monitor = "monitor";
    internal const string Link = "link";
    internal const string Reply = "spawn_reply";
    internal const string Success = "ok";
    internal const string Normal = "normal";
    internal const string Down = "DOWN";
    internal const string Process = "process";
    internal const string Invalid = "invalid_async_spawn_value";
    internal const string BadArgument = "badarg";
    internal const string NoProcess = "noproc";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
