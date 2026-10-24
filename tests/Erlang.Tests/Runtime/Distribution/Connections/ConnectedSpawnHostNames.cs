namespace Erlang.Tests;

internal static class ConnectedSpawnHostNames
{
    internal const string NodeA = "spawn_a@localhost";
    internal const string NodeB = "spawn_b@localhost";
    internal const string Kernel = "net_kernel";
    internal const string Erlang = "erlang";
    internal const string SetNode = "setnode";
    internal const string SpawnRequest = "spawn_request";
    internal const string Abandon = "spawn_request_abandon";
    internal const string Abandoned = "abandoned";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string SpawnReply = "spawn_reply";
    internal const string GroupLeader = "group_leader";
    internal const string ProcessInfo = "process_info";
    internal const string Links = "links";
    internal const string Link = "link";
    internal const string Monitor = "monitor";
    internal const string Down = "DOWN";
    internal const string Exit = "EXIT";
    internal const string TargetModule = "connected_spawn_host";
    internal const string TargetFunction = "child";
    internal const string Unrelated = "unrelated_spawn_mail";
    internal const string ChildReason = "remote_child_done";
    internal const string Release = "release_remote_child";
    internal const string Success = "ok";
    internal const string Failure = "Connected remote spawn HOST contract failed.";
    internal const int DeadlineSeconds = 10;
    internal const int PollMilliseconds = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(DeadlineSeconds);
    internal const ulong Flags = DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.Spawn | DistributionPassThroughFlags.ProcessMonitors | DistributionPassThroughFlags.Alias;
}
