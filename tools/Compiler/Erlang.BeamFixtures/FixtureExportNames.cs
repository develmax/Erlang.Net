namespace BeamFixtureExport;

internal static class FixtureExportNames
{
    public const string ExecutableExtension = ".exe";
    public const string BinaryDirectory = "bin";
    public const string OracleExecutable = "erl";
    public const string CompilerExecutable = "erlc";
    public const string ReleasesDirectory = "releases";
    public const string VersionFilename = "OTP_VERSION";
    public const string BuildEvidenceFilename = "source-build-evidence.json";
    public const string BuildLogFilename = "source-build.log";
    public const string OracleVersionLogPrefix = "oracle-version";
    public const string WarningsAsErrors = "-Werror";
    public const string DebugInformation = "+debug_info";
    public const string OutputDirectoryOption = "-o";
    public const string SourceExtension = ".erl";
    public const string BeamExtension = ".beam";
    public const string DisassemblyExtension = ".disassembly.term";
    public const string CompilationLogSuffix = "-compile";
    public const string DisassemblyLogSuffix = "-disassemble";
    public const string ManifestFilename = "manifest.json";
    public const string ProvenanceScope = "Supplied source-build evidence is retained and hash-bound; this exporter does not independently rebuild or attest the compiler";
    public const string VersionExpression = "io:format(\"~p~n\",[{erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(system_version)}]),halt().";
}
