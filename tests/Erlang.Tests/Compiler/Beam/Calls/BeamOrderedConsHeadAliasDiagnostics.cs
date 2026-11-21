namespace Erlang.Tests;

internal static class BeamOrderedConsHeadAliasDiagnostics
{
    internal const string Reconstruction = "An exact list-head reconstruction must reuse its matched tuple while a changed token must be constructed.";
    internal const string BranchResult = "Tuple alias substitution must preserve the selected clause, tail call, and source stack.";
}
