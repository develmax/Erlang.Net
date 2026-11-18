namespace Erlang.Tests;

internal static class BeamEntryExternalTailDiagnostics
{
    internal const string BranchFrame = "An ordinary external list tail retained the entry frame or a trapping exit lost its frame.";
    internal const string TrappingHeapFrame = "A trapping tail must allocate its empty frame at the argument heap check.";
}
