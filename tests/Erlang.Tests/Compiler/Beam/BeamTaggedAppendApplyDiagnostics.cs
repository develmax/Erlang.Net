namespace Erlang.Tests;

internal static class BeamTaggedAppendApplyDiagnostics
{
    internal const string FramePlacement = "Expected a frame exclusively on the append branch of the tagged dispatch.";
    internal const string Publication = "The tagged dispatch could not be admitted for BEAM execution.";
    internal const string BranchResult = "A tagged append dispatch branch returned the wrong result.";
    internal const string UncheckedInput = "A malformed tuple reached the tagged append dispatch.";
}
