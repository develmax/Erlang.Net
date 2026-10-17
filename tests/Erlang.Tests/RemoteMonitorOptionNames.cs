namespace Erlang.Tests;

internal static class RemoteMonitorOptionNames
{
    internal const string SpawnTest = "runtime/remote-spawn-opt-monitor-atom-preserves-custom-tag";
    internal const string RequestTest = "runtime/remote-spawn-request-monitor-atom-preserves-custom-tag";
    internal const string NodeA = "monitor_order_a@localhost";
    internal const string NodeB = "monitor_order_b@localhost";
    internal const string Module = "remote_monitor_option_order";
    internal const string Child = "child";
    internal const string Erlang = "erlang";
    internal const string Spawn = "spawn_opt";
    internal const string Request = "spawn_request";
    internal const string Monitor = "monitor";
    internal const string Tag = "tag";
    internal const string Custom = "custom_monitor_order";
    internal const string Down = "DOWN";
    internal const string Release = "release_monitor_child";
    internal const string Process = "process";
    internal const string Normal = "normal";
    internal const string Reply = "spawn_reply";
    internal const string Success = "ok";
    internal const string Failure = "Remote monitor option ordering contract failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
