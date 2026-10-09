internal static class ToolDiagnostics
{
    public static string ArchiveExit(int exitCode, string error) => $"git archive exited {exitCode}: {error}";

    public const string BaselineMismatch = "Baseline tag does not resolve to pinned commit";
    public const string InputOutputPaths = "Expected input and output paths, optionally a nullable context for preprocessing";
    public const string InventoryArguments = "inventory requires the OTP repo and output directory";
    public const string UnknownCommand = "Unknown command";
    public const string Usage = "Usage: erlang compile <input.erl> <output.cs> | preprocess <input.cs> <output.cs> | inventory <otp-repo> <output-directory>";
}
