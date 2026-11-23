namespace Erlang.Tests;

internal static class BeamTerminalTaggedProjectionDiagnostics
{
    internal const string LivePrefixMismatch =
        "A tagged case return did not root its selector or nested tuple until after the heap check.";
}
