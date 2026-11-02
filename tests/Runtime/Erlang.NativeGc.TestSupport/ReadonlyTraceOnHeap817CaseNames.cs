namespace Erlang.NifNativeBackend;

public static class ReadonlyTraceOnHeap817CaseNames
{
    public const string Physical = "native/readonly-trace817-onheap-source-gc-and-selected-native-shares";
    internal const string Failure = "Actual on_heap source frame custody, GC preservation or selected trace sharing failed.";
    internal const string NormalPayload = "normal_payload";
    internal const string PriorityPayload = "priority_payload";
    internal const int BinaryBytes = 512;
    internal const int TraceFields = 4;
    internal const int InfoReplyFields = 2;
    internal const int OutputPhysicalWords = 16;
    internal const int ReferenceOffset = 0;
    internal const int ExpectedTraceCount = 2;
    internal const int ExpectedQueuedEntries = 3;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
