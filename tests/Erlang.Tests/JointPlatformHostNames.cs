namespace Erlang.Tests;

internal static class JointPlatformHostNames
{
    public const string Test = "runtime/joint-display-origin-token-monitor-and-public-exit";
    public const string Module = "joint_platform_host";
    public const string Held = "held";
    public const string Label = "joint_host";
    public const string ExitLabel = "joint_exit";
    public const string TrapExit = "trap_exit";
    public const string Stop = "joint_stop";
    public const int ExpectedOwnerReplies = 2;
    public const int ExpectedTraceRecords = 3;
    public const int ExpectedCallerQueueEdges = 4;
    public const int ExpectedCleanupQueueEdges = 2;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
