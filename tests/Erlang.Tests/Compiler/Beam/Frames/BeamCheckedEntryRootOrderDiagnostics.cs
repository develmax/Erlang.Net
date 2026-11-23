namespace Erlang.Tests;

internal static class BeamCheckedEntryRootOrderDiagnostics
{
    internal const string RootPermutation = "Expected the first checked-entry root in the highest physical Y slot.";
    internal const string DelayedCopy = "Expected a total projection before preserving the previous call result.";
    internal const string SeparateOwner = "Keep the checked-entry layout when the rejected edge owns a separate frame.";
    internal const string HeapBoundary = "Keep the saved call result before a heap checkpoint.";
    internal const string SavedFirstArgument = "Keep a call setup that reads the saved value as another argument.";
}
