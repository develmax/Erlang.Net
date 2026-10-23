namespace Erlang;

internal static class InterpreterCodeNativeNames
{
    internal const string Module = "code";
    internal const string CoverageSupport = "coverage_support";
    internal static readonly (string Function, int Arity)[] BadArgumentSignatures =
    [
        ("get_coverage_mode", 0),
        ("get_coverage_mode", 1),
        ("set_coverage_mode", 1),
        ("get_coverage", 2),
        ("reset_coverage", 1),
        ("get_debug_info", 1)
    ];
}
