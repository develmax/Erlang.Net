namespace Erlang.Tests;

internal static class RuntimeNodeControlHostNames
{
    public const string Test = "runtime/node-control-named-unconnected-real-down-and-self-noop";
    public const string Node = "control@local";
    public const string Peer = "peer@invalid";
    public const string Failure = "Named unconnected node control must deliver actual nodedown without fabricating connected peers";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
