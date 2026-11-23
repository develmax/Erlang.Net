namespace Erlang.Tests;

internal static class BeamSeparatedTailBranchDiagnostics
{
    internal const string Instructions = "Expected independent frames only for call branches that need them.";
    internal const string Result = "A separate tail branch returned the wrong result.";
    internal const string InvalidInputAccepted = "A separate tail branch accepted an invalid pattern.";
}
