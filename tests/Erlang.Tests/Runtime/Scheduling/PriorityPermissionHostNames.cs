namespace Erlang.Tests;

public static class PriorityPermissionHostNames
{
    public const string Test = "runtime/priority-permission-unlink-generation-admission";
    public const string Failure = "Priority permission identity or unlink admission invariant failed.";
    public const int Rounds = 64;
    public const int PendingLinkDeathRounds = 16;
    public const int DeadPeerRelinkRounds = 8;
    public const string ProcessFlag = "process_flag";
    public const string TrapExit = "trap_exit";
    public const string LinkIdentityProperty = "LinkIdentities";
    public const string PendingUnlinkProperty = "PendingUnlink";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
