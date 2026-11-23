namespace Erlang.Tests;

internal static class BeamDisjointFrameReuseDiagnostics
{
    internal const string Compiler = "Erlang.Compiler.BeamModuleCompiler";
    internal const string Plan = "PlanDisjointFrames";
    internal const string ReusableFrame = "A closed branch frame retained two slots for disjoint value lifetimes.";
    internal const string OverlappingValues = "A branch frame merged slots whose values are both used by the same test.";
}
