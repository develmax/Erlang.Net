namespace BeamFixtureExport;

internal static class FixtureExecutionDiagnostics
{
    public static string EtfMismatch(string module) => "Actual/independent expected ETF differ: " + module;
}
