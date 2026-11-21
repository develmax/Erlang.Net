namespace Erlang.Tests;

internal static class BeamOrderedBranchConsDiagnostics
{
    internal const string ConstructionSchedule = "An ordinary branch must retain its inputs and defer pure list construction until after the tail call.";
    internal const string EvaluationOrder = "The terminal list must preserve the input, source behavior, and order of observable calls.";
}
