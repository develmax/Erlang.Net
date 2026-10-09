namespace Erlang.Differential;

internal static class OracleDiagnostics
{
    public static string ProcessExit(int exitCode, string stderr, string stdout) => $"Oracle exited {exitCode}: {stderr} {stdout}";

    public static string VersionMismatch(string version) => "Oracle version mismatch: " + version;
}
