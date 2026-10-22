namespace Erlang.Tests;

internal static class BoundedBifHostNames
{
    internal const string Fixture = "bounded_bif_contract.erl";
    internal const string Module = "bounded_bif_contract";
    internal const string TestName = "runtime/bounded-bif-contract-guards-stack-depth-identity-and-big-range";
    internal const string Success = "ok";
    internal const string DefaultDepth = "default";
    internal const string Erlang = "erlang";
    internal const string Invalid = "invalid";
    internal const string InvalidBound = "invalid_bound";
    internal const string Error = "error";
    internal const string BadArg = "badarg";
    internal const string ErrorInfo = "error_info";
    internal const string FormatterKey = "module";
    internal const string Formatter = "erl_erts_errors";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string Failure = "Bounded BIF changed argument validation, source caller metadata, guard rejection, identity or arbitrary-precision range behavior.";
    internal const int FunctionCount = 4;
    internal const int DepthCount = 5;
    internal const int DefaultDepthLimit = 8;
    internal const int RangeMaximum = 10;
    internal const int CallerArity = 1;
    internal static readonly string[] Functions = ["hd", "tl", "tuple_size", "is_integer"];
    internal static readonly string[] Callers = ["hd_context", "tl_context", "tuple_context", "integer_context"];
    internal static readonly int[] Lines = [13, 14, 15, 16];
    internal static readonly int[] Limits = [8, 1, 1, 2, 8];
    internal static readonly int[] ExplicitDepths = [0, 1, 2, 8];
}
