internal static class GenServerValidationDiagnostics
{
    internal const string PreparePrefix = "Prepare: ";
    internal const string ScenarioPrefix = "Scenario: ";
    internal const string Usage = "Usage: Erlang.GenServerCompatibility <repository> <oracle-directory> Managed|Beam [scenario]";
    internal const string PublicationFailed = "gen_server image publication failed";
    internal const string BackendOwnerMismatch = "gen_server selected backend owner mismatch";
    internal const string BootEnded = "Original code_server boot ended prematurely";
    internal const string AdmissionFailed = "gen_server immutable oracle admission failed";

    internal static string Summary(
        string backend,
        int passed,
        int total,
        int scenarios,
        int failures
    )
        => $"{backend}: {passed}/{total} contracts; {scenarios} scenarios; {failures} failures";
}
