namespace Erlang.NifNativeBackend;

public static class ReadonlyTracePriority817CaseNames
{
    public const string Physical = "native/readonly-trace817-real-priority-placement-and-trace-order";
    internal const string Failure = "Actual priority alias Receive placement, native ownership or ordering failed.";
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
