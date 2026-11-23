namespace Erlang.Tests;

internal static class BeamTransitiveRecordProofDiagnostics
{
    internal const string ForwardedRecordChanged = "The relay changed a valid record result.";
    internal const string UncheckedRecordAccepted = "A relay without a record contract accepted an invalid record.";

    internal static string RecordCheckCount(int expected, int actual) =>
        $"Expected {expected} tagged-tuple checks in the relay fixture, found {actual}.";
}
