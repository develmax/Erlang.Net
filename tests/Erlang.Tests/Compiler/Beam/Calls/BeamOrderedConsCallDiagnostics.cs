namespace Erlang.Tests;

internal static class BeamOrderedConsCallDiagnostics
{
    internal const string ConstructionSchedule = "Pure list heads and their outer tuple must be constructed after the tail call in one heap reservation.";
    internal const string EvaluationOrder = "The cons and tuple results must preserve tail identity, source binding, and effect order.";
}
