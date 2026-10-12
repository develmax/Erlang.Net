namespace Erlang.Tests;

public static class FunctionResolutionHostDiagnostics
{
    public const string Implicit = "Standalone execution implicitly called a non-auto-imported export";
    public const string Qualified = "Qualified or dynamic execution failed to call the actual export";
}
