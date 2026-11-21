namespace Erlang.Tests;

internal static class BeamCheckedCopiedTupleSourcesDiagnostics
{
    internal const string Rewrite = "Expected only the proved tuple source operands to change.";
    internal const string AlreadyRewritten = "Expected rewritten checked tuple sources to remain stable.";
    internal const string UnprovedCopy = "Expected unchecked tuple copy operands to remain unchanged.";
}
