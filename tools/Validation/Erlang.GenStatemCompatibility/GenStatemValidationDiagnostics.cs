internal static class GenStatemValidationDiagnostics
{
    internal const string PreparePrefix = "Prepare: ";
    internal const string ScenarioPrefix = "Scenario: ";
    internal const string Usage = "Usage: Erlang.GenStatemCompatibility <repository> <oracle-directory> Managed|Beam [scenario]";
    internal const string PublicationFailed = "gen_statem image publication failed";
    internal const string BackendOwnerMismatch = "gen_statem selected backend owner mismatch";
    internal const string BootEnded = "Original code_server boot ended prematurely";
    internal const string AdmissionFailed = "gen_statem immutable oracle admission failed";

    internal static string Summary(
        string backend,
        int passed,
        int total,
        int scenarios,
        int failures
    )
        => $"{backend}: {passed}/{total} contracts; {scenarios} scenarios; {failures} failures";
}
