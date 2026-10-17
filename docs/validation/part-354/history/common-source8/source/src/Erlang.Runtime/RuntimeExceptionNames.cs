namespace Erlang;

internal static class RuntimeExceptionNames
{
    internal const string Error = "error";
    internal const string Exit = "exit";
    internal const string Throw = "throw";
    internal const string ErrorInfo = "error_info";
    internal const string Module = "module";
    internal static string? Formatter(string module) => module switch
    {
        "erlang" or "erts_internal" or "atomics" or "counters" or "persistent_term" => "erl_erts_errors",
        "code" or "os" => "erl_kernel_errors",
        "binary" or "ets" or "lists" or "maps" or "math" or "re" or "unicode" => "erl_stdlib_errors",
        _ => null
    };
}
