namespace Erlang.Tests;

internal static class BeamCheckedResultRecordContinuationDiagnostics
{
    internal const string MissingEntry = "The function consuming the checked result is missing.";
    internal const string ResultFrame = "The checked result must stay live in X0 across the cons heap test and on the badrecord edge.";
    internal const string ProjectionLiveness = "The three checked projections belong in outgoing X registers, with the consumed stack slot trimmed.";
    internal const string ColdRecordOrder = "The short badrecord exit and its line belong after the ordinary grouped entry arms.";
    internal const string ResultOrEffectOrder = "The checked record returned the wrong result, error or process-dictionary effects.";
}
