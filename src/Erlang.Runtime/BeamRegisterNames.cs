namespace Erlang;

internal static class BeamRegisterNames
{
    public const string Call = "call";
    public const string Erlang = "erlang";
    internal const string OsModule = "os";
    internal const string GetEnvironment = "getenv";
    public const string Add = "+";
    public const string Subtract = "-";
    public const string Unsupported = "Unsupported reachable BEAM register instruction";
    public const string Uninitialized = "Uninitialized BEAM register";
    public const string DiagnosticBudgetExceeded = "Explicit artifact diagnostic instruction budget exhausted";
}
