namespace Erlang.Tests;

internal static class AllocatorQueryMailboxHostNames
{
    internal const string Test = "runtime/allocator-query-owned-mailbox-transport";
    internal const string ProviderType = "Erlang.AllocatorQueryProvider";
    internal const string MailboxType = "Erlang.AllocatorQueryMailbox";
    internal const string Gather = "Gather";
    internal const string OperationReceive = "ReceiveForOperationAsync";
    internal const string WaitingField = "WaitingForMessage";
    internal const string UnwoundProperty = "Unwound";
    internal const string ReferenceCounterField = "nextRef";
    internal const string First = "sdk_transport_first";
    internal const string Unknown = "sdk_transport_unknown";
    internal const string Marker = "allocator_transport_marker";
    internal const string Earlier = "allocator_transport_earlier";
    internal const string Later = "allocator_transport_later";
    internal const string Instance = "instance";
    internal const string CleanupKey = "AllocatorQueryCleanupFailure";
    internal const string Kill = "kill";
    internal const string Failure = "Allocator mailbox transport assertion failed.";
    internal const string ProducerFailure = "Controlled allocator producer failure.";
    internal const string CleanupFailure = "Controlled allocator cleanup failure.";
    internal const int HaltStatus = 23;
    internal const int TraceLabel = 31;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
