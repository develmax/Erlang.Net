namespace Erlang.NifNativeBackend;

public static class MailboxBackingCaseNames
{
    public const string OnHeap = "runtime/mailbox-actual-on-heap-value-and-host-escape";
    public const string OffHeap = "runtime/mailbox-actual-off-heap-value-and-host-escape";
    internal const string StorageMethod = "Storage";
    internal const string Tag = "mailbox_backing_tag";
    internal const string Tail = "mailbox_backing_tail";
    internal const string GateField = "gate";
    internal const string MessagesField = "messages";
    internal const string ValueProperty = "Value";
    internal const string CustodyProperty = "Custody593";
    internal const string WorkField = "work";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(8);
}
