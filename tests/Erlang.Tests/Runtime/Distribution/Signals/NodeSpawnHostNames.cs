namespace Erlang.Tests;

internal static class NodeSpawnHostNames
{
    public const string Test = "runtime/node-spawn-atomic-down-no-remote-callback-and-slot-release";
    public const string Node = "named@host";
    public const string Missing = "missing@host";
    public const string Module = "node_spawn_host";
    public const string Callback = "callback";
    public const string Failure = "Node spawn lifecycle contract failed";
    public const string Down = "DOWN";
    public const string Process = "process";
}
