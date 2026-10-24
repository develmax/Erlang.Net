namespace Erlang.Tests;

internal static class MailboxSelectionCheckNames
{
    internal const string Test = "Mailbox callbacks outside lock with precedence, identity and lifecycle revalidation";
    internal const string Failed = "Mailbox snapshot selection contract failed";
    internal const string Seed = "mailbox_snapshot_seed";
    internal const string Tail = "mailbox_snapshot_tail";
    internal const string Urgent = "mailbox_snapshot_urgent";
    internal const string Unmatched = "mailbox_snapshot_unmatched";
    internal const string Same = "mailbox_snapshot_same";
    internal const string Completed = "mailbox_snapshot_completed";
    internal const int SafetyDeadlineSeconds = 5;
    internal const int HaltStatus = 17;
    internal const int SingleMessage = 1;
    internal const int RevalidatedSelectors = 2;
    internal const int WarmupMessages = 16;
    internal const int SmallFifoMessages = 1024;
    internal const int LargeFifoMessages = 2048;
    internal const int LinearAllocationAllowance = 3;
    internal const int FixedAllocationAllowanceBytes = 1 << 20;
    internal const int BoundedProducerMaximum = 128;
    internal const int FiniteTimeoutMilliseconds = 20;
    internal const int SelectorWorkMilliseconds = 1;
    internal const int SelectorSpinIterations = 16;
    internal const int ProducerWatchdogMilliseconds = 500;
    internal const string Erlang = "erlang";
    internal const string ProcessFlag = "process_flag";
    internal const string SaveCalls = "save_calls";
    internal const int SavedCallCapacity = 16;
    internal const string SavedCallsProperty = "SavedCalls";
    internal const string SnapshotMethod = "Snapshot";
    internal const string ReceiveMarker = "receive";
}
