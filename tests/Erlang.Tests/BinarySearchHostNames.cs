namespace Erlang.Tests;

internal static class BinarySearchHostNames
{
    internal const string Failure = "Binary search source contract or resource ownership was not preserved.";
    internal const string ResourceTest = "runtime/binary/search/compiled-resource-owner-etf-and-forgery";
    internal const string OptionsTest = "runtime/binary/search/options-leftmost-longest-and-trimming";
    internal const string CooperativeTest = "runtime/binary/search/cooperative-cancellation-unwind";
    internal const string CompiledTest = "compiler/binary/search/seven-contracts-and-error-callers";
    internal const string Fixture = "binary_search_contract.erl";
    internal const string Success = "ok";
    internal const string Done = "done";
    internal const string Single = "bm";
    internal const string Multiple = "ac";
    internal const string Scope = "scope";
    internal const string Global = "global";
    internal const string Trim = "trim";
    internal const string TrimAll = "trim_all";
    internal const string Tail = "tail";
    internal const string Invalid = "invalid";
    internal const string NoMatch = "nomatch";
    internal const string ErrorInfo = "error_info";
    internal const string Module = "module";
    internal const string Formatter = "erl_stdlib_errors";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string Normal = "normal";
    internal const int SubjectBytes = 1048576;
    internal const int PatternBytes = 1024;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
