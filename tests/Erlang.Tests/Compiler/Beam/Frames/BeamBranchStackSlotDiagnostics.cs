namespace Erlang.Tests;

internal static class BeamBranchStackSlotDiagnostics
{
    internal const string LastProjectionRead = "Expected the list projection to reuse the slot after its last read.";
    internal const string BranchJoinFrame = "Expected the branch join to preserve the reference stack frame.";
}
