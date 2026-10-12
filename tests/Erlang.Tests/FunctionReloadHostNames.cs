namespace Erlang.Tests;

public static class FunctionReloadHostNames
{
    public const string Test = "runtime/owned-local-function-matching-code-retarget";
    public const string Module = "function_reload_host";
    public const string Callback = "function_reload_callback";
    public const string Probe = "probe";
    public const string Make = "make";
    public const string Kept = "kept";
    public const string Custom = "custom";
    public const string Broken = "Owned local function retarget or captured old code differs";
}
