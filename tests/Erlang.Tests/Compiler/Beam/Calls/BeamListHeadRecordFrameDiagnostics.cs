namespace Erlang.Tests;

internal static class BeamListHeadRecordFrameDiagnostics
{
    internal const string EntryOwnership = "The checked list head must reach its own call frame after extraction and its record test.";
    internal const string FallbackOwnership = "The rejected record guard must enter the original fallthrough with its own frame requirements.";
    internal const string BodyFailure = "A body record failure must keep the earlier call and the badrecord exception.";
}
