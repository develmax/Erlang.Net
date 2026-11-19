namespace Erlang.Tests;

internal static class BeamAdjacentBifResultDiagnostics
{
    internal const string RootMismatch =
        "An adjacent GC BIF result did not use X0 for its sole read or retain its stack slot for a later read.";
}
