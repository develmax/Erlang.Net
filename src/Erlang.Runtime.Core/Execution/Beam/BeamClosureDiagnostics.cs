namespace Erlang;

internal static class BeamClosureDiagnostics
{
    internal const string Context = "Captured BEAM invocation requires an actual process context";
    internal const string Frame = "Invalid zero-slot BEAM continuation frame";
    internal const string KnownTarget = "Known-target BEAM fun does not retain its linked code identity";
}
