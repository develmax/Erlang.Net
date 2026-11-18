namespace Erlang.Tests;

internal static class BeamReconstructedTupleDiagnostics
{
    internal const string Rebuilt = "A tagged arm allocated a tuple with every element unchanged.";
    internal const string Dispatch = "The tagged tuple arms did not share the arity check and dispatch.";
    internal const string BadReuse = "A tagged arm reused a tuple after changing its tag.";
}
