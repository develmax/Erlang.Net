namespace Erlang.Tests;

internal static class BeamSharedCheckedCallSpillDiagnostics
{
    internal const string Shared = "Expected both checked result edges to use the shared stack definition.";
    internal const string Unchanged = "Expected an owned or distinct result definition to keep its existing location.";
}
