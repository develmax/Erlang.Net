namespace BeamFixtureExport;

internal static class FixtureExportDiagnostics
{
    public const string MissingOracle = "Installed OTP compiler/oracle is unavailable";
    public const string InvalidEvidence = "Source-build evidence does not bind the pinned source to this installation";
    public const string BuildLogMismatch = "Source-build log digest differs from supplied provenance";
    public const string ExistingOutput = "Output directory already exists; prior evidence will not be overwritten";
    public const string InvalidForm = "Compiler output is not a BEAM IFF form";
    public const string FormLengthMismatch = "Compiler output BEAM form length differs from raw bytes";
    public const string TruncatedHeader = "Truncated chunk header";
    public const string TruncatedPayload = "Truncated chunk payload/padding";
    public const string ProcessStart = "Oracle process could not start";

    public static string InvalidVersion(string version) => "Installed OTP_VERSION must be exactly " + version;

    public static string ProcessTimeout(string executable) => $"Oracle timed out: {executable}";

    public static string ProcessExit(int code, string executable, string prefix) => $"Oracle exit {code}: {executable}; raw logs: {prefix}";
}
