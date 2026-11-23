namespace Erlang.Tests;

internal static class BeamGcBifCallResultDiagnostics
{
    internal const string ResultRootMismatch =
        "A GC BIF did not preserve its call-result root and the later use of a saved input.";
}
