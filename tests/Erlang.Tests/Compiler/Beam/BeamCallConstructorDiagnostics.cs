namespace Erlang.Tests;

internal static class BeamCallConstructorDiagnostics
{
    internal const string Instructions = "Expected a zero-slot closure call frame and sibling constructors without moves.";
    internal const string InvalidInputAccepted = "The constructor fixture accepted an invalid entry argument.";
    internal const string PrefixFrame = "Expected a zero-slot frame before the discarded calls and closure heap after both calls.";
    internal const string PrefixEffects = "The discarded calls were skipped, reordered, or ran before the entry pattern matched.";
    internal const string SiblingListRegisters = "Expected a folded suffix and one register for all remaining sibling list cells.";
    internal const string SiblingListResult = "A sibling list lost an element, its tail, or a required call.";
    internal const string Result = "The constructor fixture returned the wrong result.";
}
