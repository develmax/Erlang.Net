namespace Erlang.Tests;

internal static class SpawnMonitor431Names
{
    internal const string DemonitorTest = "runtime/spawn-monitor431-live-reply-demonitor-no-monitor-bits";
    internal const string Demonitor = "demonitor";
    internal const string LifeTest = "runtime/spawn-monitor431-no-negotiated-monitor-bits-real-child-and-node-down";
    internal const string AbandonTest = "runtime/spawn-monitor431-abandoned-real-reply-no-unsupported-demonitor";
    internal const string Failure = "Spawn monitor negotiated downgrade protocol failed";
    internal const string Internal = "erts_internal", Native = "dist_spawn_request", Request = "spawn_request", Erlang = "erlang", Target = "spawn_monitor431", Run = "run", Release = "release431", Reason = "child_exit431", Monitor = "monitor", Reply = "spawn_reply", Success = "ok", Down = "DOWN", NoConnection = "noconnection", Abandon = "spawn_request_abandon", True = "true";
    internal const ulong Flags = DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.Spawn;
}
