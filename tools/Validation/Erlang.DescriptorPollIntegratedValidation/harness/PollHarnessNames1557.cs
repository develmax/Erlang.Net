namespace Erlang;

internal static class PollHarnessNames1557
{
    internal const string CleanupSucceeded = "CleanupSucceeded";
    internal const string PhysicalEnded = "PhysicalLifetimeEnded";
    internal const string ThreadJoined = "ActualThreadJoined";
    internal const string LostWakePending = "LostWakePendingBeforeCleanup";
    internal const string All = "all";
    internal const string ConcurrentDispose = "concurrent-dispose-held-poll-entry";
    internal const string ServiceGate = "gate";
    internal const string ShutdownStarted = "shutdownStarted";
    internal const string ShutdownCompletion = "shutdown";
    internal const string WakeRead = "wakeRead";
    internal const string WakeWrite = "wakeWrite";
    internal const int ClosedDescriptor = -1;
    internal const string LostWake = "lost-wake";
    internal const string Identity = "poll-thread-identity";
    internal const string Data = "read-write-ready";
    internal const string Cancel = "cancel-native-snapshot";
    internal const string Reuse = "retire-reuse-fd";
    internal const string TwoOwners = "same-fd-two-owners";
    internal const string Invalid = "invalid-fd";
    internal const string Hangup = "hangup-eof";
    internal const string Stop = "stop-active-snapshot";
    internal const string Unstarted = "stop-before-start";
    internal const string Contract = "Actual Linux descriptor poll engine contract failed";
    internal const string AccountingGate = "gate";
    internal const string AccountingRecords = "records";
    internal const string PollType = "poll";
    internal const string ProcTask = "/proc/self/task/";
    internal const string ProcSyscall = "/syscall";
    internal const string PollSyscallText = "7";
    internal const string HexPrefix = "0x";
    internal const string KernelSource = "https://github.com/torvalds/linux/blob/v6.12/arch/x86/entry/syscalls/syscall_64.tbl#L19";
    internal const int NfdsToken = 2;
    internal const int ProbeMilliseconds = 1;
    internal const int WakeOnlyNfds = 1;
    internal const int OneWaitNfds = 2;
    internal const int TwoWaitNfds = 3;
    internal const int InvalidFd = int.MaxValue;
    internal const byte DataByte = 65;
    internal const int PipeReadIndex = 0;
    internal const int PipeWriteIndex = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan LostWakeDeadline = TimeSpan.FromSeconds(3);
    internal static readonly string[] Cases = [Identity, Data, Cancel, Reuse, TwoOwners, Invalid, Hangup, Stop, Unstarted, LostWake, ConcurrentDispose];
}
