namespace Erlang.Tests;

internal static class BeamWideIntegerConstructionDiagnostics
{
    internal const string Layout = "Expected a compact integer prefix and the original finite string suffix.";
    internal const string Value = "The compact bitstring construction changed the resulting bits.";
    internal const string Error = "The compact bitstring construction changed the dynamic field error.";
}
