namespace Erlang.Tests;

internal static class BeamLeafFrameDiagnostics
{
    internal const string EntryTestFrame = "Expected entry tests to permit a frameless leaf and preserve the frame across a call.";
    internal const string ListHeapRoots = "A leaf list failed to retain its initialized X roots without a stack frame.";
}
