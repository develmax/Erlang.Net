namespace Erlang.Tests;

internal static class BeamClosedAtomDispatchDiagnostics
{
    internal const string ConstructedFallbackRegister = "The atom dispatch fallback must construct its tuple in the call argument register.";
    internal const string MixedResults = "Expected a single dispatch table for literal results and tail calls.";
    internal const string LeadingTailBranches = "Leading atom clauses did not dispatch to their local and external tail branches.";
    internal const string LeadingTailBodyOrder = "Independent atom tail bodies must follow descending atom order.";
    internal const string CompleteTailSelector = "A final literal clause did not share the leading clauses' dispatch table.";
}
