namespace Erlang.Tests;

internal static class BeamMixedConstructorCaseDiagnostics
{
    internal const string Partition = "The tuple test must lead to the atom branch on a type miss and to the case fallback on an arity miss.";
    internal const string ClauseOrder = "The case must preserve the source order of overlapping clauses and the selected values.";
    internal const string FailureValueAndLocation = "The case failure must preserve the rejected selector and its source location.";
}
