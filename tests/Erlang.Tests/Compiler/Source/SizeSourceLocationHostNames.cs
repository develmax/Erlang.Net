namespace Erlang.Tests;

internal static class SizeSourceLocationHostNames
{
    internal const string Fixture = "size_source_location_contract.erl";
    internal const string Module = "size_source_location_contract";
    internal const string TestName = "runtime/size-bif-caller-source-location-depth-and-guards";
    internal const string Erlang = "erlang";
    internal const string Invalid = "invalid";
    internal const string Success = "ok";
    internal const string Error = "error";
    internal const string BadArg = "badarg";
    internal const string BadMap = "badmap";
    internal const string File = "file";
    internal const string Line = "line";
    internal const string ErrorInfo = "error_info";
    internal const string FormatterKey = "module";
    internal const string Formatter = "erl_erts_errors";
    internal const string Failure = "Size BIF lost its compiled caller source location or changed guard/error semantics.";
    internal static readonly string[] Functions = ["tuple_size", "bit_size", "byte_size", "map_size"];
    internal static readonly string[] Callers = ["tuple_context", "bits_context", "bytes_context", "map_context"];
    internal static readonly int[] Lines = [13, 14, 15, 16];
    internal const int FunctionCount = 4;
    internal const int DepthCount = 5;
    internal const int CallerArity = 1;
}
