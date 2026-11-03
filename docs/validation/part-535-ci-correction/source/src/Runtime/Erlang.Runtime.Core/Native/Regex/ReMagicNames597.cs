namespace Erlang;

internal static class ReMagicNames597
{
    internal const int PrefixBytes = 56;
    internal const int AllocationBytes = 64;
    internal const int BinaryDataOffset = 32;
    internal const int PatternReferenceIndex = 4;
    internal const int NumberCount = 3;
    // Own allocator dispatch tag, not a claim of OTP's build-specific enum ID.
    internal const int AllocatorDispatch = 597;
    internal const string Invalid = "Invalid compiled-regex backing or custody.";
    internal const string ActiveDispose = "Dispose the runtime after guest regex roots retire; this engine cannot join its own active invocation.";
    internal const string UnpublishedDispose599 = "Only a fresh unpublished regex engine with no code or active operation can use installation rollback.";
    internal const string Closed = "Compiled-regex backing is closed.";
    internal const string Underflow = "Compiled-regex backing ownership underflow.";
    internal const string QueueFailure = "Compiled-regex retirement could not be scheduled.";
    internal const string DestructorFailure = "Compiled-regex physical destructor failed.";
}
