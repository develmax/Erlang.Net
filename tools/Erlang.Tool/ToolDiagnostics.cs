internal static class ToolDiagnostics
{
    public static string ArchiveExit(int exitCode, string error) => $"git archive exited {exitCode}: {error}";

    public const string BaselineMismatch = "Baseline tag does not resolve to pinned commit";
    public const string InputOutputPaths = "Expected input and output paths, optionally a nullable context for preprocessing";
    public const string InventoryArguments = "inventory requires the OTP repo and output directory";
    public const string UnknownCommand = "Unknown command";
    public const string ReadinessArguments = "readiness requires docs directory, test report and output directory";
    public const string ReadinessRegistryMismatch = "Readiness registry differs from the actual runtime or lacks passed direct MFA tests";
    public const string ReadinessInvalidInput = "Readiness requires a passing report and matching pinned inventory baseline";
    public const string Usage = "Usage: erlang compile <input.erl> <output.cs> | preprocess <input.cs> <output.cs> | inventory <otp-repo> <output-directory> | readiness <docs-directory> <test-report> <output-directory>";
}
