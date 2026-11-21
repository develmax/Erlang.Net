namespace Erlang.Tests;

internal static class BeamGroupedCallProjectionDiagnostics
{
    internal const string Frame = "The call-only tuple fields must leave one saved state slot and two entry roots.";
    internal const string RegisterSchedule = "Expected the checked fields in field order and the outgoing argument cycle in X registers.";
    internal const string RetainedField = "The live return field and state must survive the effect call.";
    internal const string Effects = "The grouped entry returned the wrong value or changed the effects or their order.";
}
