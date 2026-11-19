namespace Erlang.Tests;

internal static class BeamMapCallContinuationDiagnostics
{
    internal const string MapRootsMismatch =
        "A map field call did not preserve its result in X0 or changed a map value required by a later case arm.";
}
