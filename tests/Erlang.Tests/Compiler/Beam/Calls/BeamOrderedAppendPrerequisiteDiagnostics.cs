namespace Erlang.Tests;

internal static class BeamOrderedAppendPrerequisiteDiagnostics
{
    internal const string ConstructionSchedule = "Expected append operand constructors after their ordinary prerequisite calls.";
    internal const string EvaluationOrder = "Append prerequisite evaluation changed the returned term, failure, or call order.";
}
