namespace Erlang.Tests;

internal static class BeamTerminalEffectfulCaseCarrierDiagnostics
{
    internal const string Carrier = "The effectful return arm must keep the checked tuple across the call.";
    internal const string Control = "An arm outside the terminal tuple reuse proof must retain its existing construction.";
    internal const string TypedEntry = "The saved call result must precede the disjoint tuple guard and atom return.";
}
