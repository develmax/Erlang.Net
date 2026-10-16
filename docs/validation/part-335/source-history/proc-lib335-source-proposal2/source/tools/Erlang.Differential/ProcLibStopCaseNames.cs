namespace Erlang.Differential;

public static class ProcLibStopCaseNames
{
    public const string Module = "proc_lib";
    public const string Sys = "sys";
    public const string Stop = "stop";
    public const string Terminate = "terminate";
    public const string Mismatch = "proc_lib stop and sys terminate contract mismatch";
    public const string DefaultNormal = "proc-lib-stop-default-normal";
    public const string ExactReason = "proc-lib-stop-exact-reason";
    public const string EqualExitBeforeAck = "proc-lib-stop-equal-exit-before-ack";
    public const string DifferentExitBeforeAck = "proc-lib-stop-different-exit-before-ack";
    public const string AckThenDifferentExit = "proc-lib-stop-ack-then-different-exit";
    public const string MissingProcess = "proc-lib-stop-missing-process";
    public const string MissingName = "proc-lib-stop-missing-name";
    public const string StopSelf = "proc-lib-stop-stop-self";
    public const string NamedStop = "proc-lib-stop-named-stop";
    public const string NamedTupleStop = "proc-lib-stop-named-tuple-stop";
    public const string SysReturnsBeforeDeath = "proc-lib-stop-sys-returns-before-death";
    public const string StopWaitsForDeath = "proc-lib-stop-stop-waits-for-death";
    public const string TimeoutPreservesTarget = "proc-lib-stop-timeout-preserves-target";
    public const string AliasDropsLateReply = "proc-lib-stop-alias-drops-late-reply";
    public const string SysMissingName = "proc-lib-stop-sys-missing-name";
    public const string SysSelf = "proc-lib-stop-sys-self";
    public const string ViaRegistry = "proc-lib-stop-via-registry";
    public const string ViaMissing = "proc-lib-stop-via-missing";
    public const string SysDisconnected = "proc-lib-stop-sys-disconnected";
    public const string MailboxOrder = "proc-lib-stop-mailbox-order";
    public const string SysInvalidTimeout = "proc-lib-stop-sys-invalid-timeout";
    public const string StopInvalidTimeout = "proc-lib-stop-stop-invalid-timeout";
    public const string GenServerAckBeforeCallback = "proc-lib-stop-gen-server-ack-before-callback";
    public const string GenServerStopWaitsForCallback = "proc-lib-stop-gen-server-stop-waits-for-callback";
    public const string SupervisorAckBeforeChildShutdown = "proc-lib-stop-supervisor-ack-before-child-shutdown";
    public const string AfterAckTimeoutWaitsForRealDown = "proc-lib-stop-after-ack-timeout-waits-for-real-down";
    public const string OneTimeoutBudget = "proc-lib-stop-one-timeout-budget";
    public const string LocalInfinityNoConnection = "proc-lib-stop-local-infinity-noconnection-reason";
    public const string LocalFiniteNoConnection = "proc-lib-stop-local-finite-noconnection-translation";
}
