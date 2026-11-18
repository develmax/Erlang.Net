namespace Erlang.Tests;

internal static class BeamLeafFrameDiagnostics
{
    internal const string EntryTestFrame = "Expected entry tests to permit a frameless leaf and preserve the frame across a call.";
    internal const string ListHeapRoots = "A leaf list failed to retain its initialized X roots without a stack frame.";
    internal const string DistinctTupleFallback = "Tuple fallback lost a different record field projection.";
    internal const string CombinedLeafTail = "A leaf local tail call kept a stack frame for its list arguments.";
}
