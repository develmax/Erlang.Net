namespace Erlang.NifNativeBackend;

public static class PendingSignalMailboxBackingCaseNames
{
    public const string ExitOnHeap = "mailbox/language-exit-pending-on-heap-backing";
    public const string ExitOffHeap = "mailbox/language-exit-pending-off-heap-backing";
    public const string DownOnHeap = "mailbox/monitor-down-pending-on-heap-backing";
    public const string DownOffHeap = "mailbox/monitor-down-pending-off-heap-backing";
}
