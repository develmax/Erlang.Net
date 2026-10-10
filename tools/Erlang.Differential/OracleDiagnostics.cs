namespace Erlang.Differential;

internal static class OracleDiagnostics
{
    public const string InvalidResultFrame = "Oracle output must contain exactly one complete ETF result frame";
    public const string PlatformAssembliesMissing = "Trusted platform assembly inventory is unavailable";
    public const string GeneratedDefinitionMissing = "Generated assembly does not expose its module definition";

    public static string GeneratedCompilation(IEnumerable<string> errors) => "Generated C# compilation failed: " + string.Join(Environment.NewLine, errors);

    public static string GeneratedExecution(Term reason) => "Generated module process failed: " + reason;

    public static string ProcessExit(int exitCode, string stderr, string stdout) => $"Oracle exited {exitCode}: {stderr} {stdout}";

    public static string VersionMismatch(string version) => "Oracle version mismatch: " + version;
}
