namespace Erlang.Tests;

internal static class BeamLateCallKillOrderDiagnostics
{
    internal const string Order = "Expected the unchanged move and line after the independent frame kills.";
    internal const string AlreadyOrdered = "Expected the already scheduled call to remain unchanged.";
    internal const string ClearedSource = "Expected the call to read a stack slot before it was cleared.";
    internal const string TypedSource = "Expected the typed stack source to retain its schedule.";
    internal const string LiteralSource = "Expected the final stack-load pass to retain a literal move.";
    internal const string Collector = "Expected argument setup to remain ordered around a heap test.";
    internal const string NoArgument = "Expected a zero-argument call to retain its preceding move.";
    internal const string Protected = "Expected argument setup in a protected region to retain its schedule.";
    internal const string Debug = "Expected argument setup in a debug-instrumented function to retain its schedule.";
}
