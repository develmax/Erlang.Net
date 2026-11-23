namespace Erlang.Tests;

internal static class BeamProvenCatchPreparationDiagnostics
{
    internal const string ProvenBoundary = "The proven append must run before the catch in a frame with one reusable stack slot.";
    internal const string UnknownOperand = "The unknown append must retain its caught badarg and source location.";
    internal const string ProtectedEffect = "The effect call must retain its caught error and source location.";
    internal const string PreparedValues = "The caught effect received incorrect prepared arguments.";
}
