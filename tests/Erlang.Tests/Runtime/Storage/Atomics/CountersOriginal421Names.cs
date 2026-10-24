namespace Erlang.Tests;

internal static class CountersOriginal421Names
{
    internal const string Test = "oracle/compiled-module/counters421-all192-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.CountersOriginal421.", Manifest = "manifest.json";
    internal const string Internal = "erts_internal", Runtime = "erlang", File = "prim_file";
    internal const string Module = "beam_counters421", FacetBeam = "erts_internal.beam", WorkerBeam = "beam_counters421.beam";
    internal const string Run = "run_child", Stacks = "run_stacks_child", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library421-counters-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original internal owner-backed native cohort differs; inspect full raw and typed observations";
    internal const int Rows = 32, WholeRows = 192, CapturedNatives = 5, PublicExports = 7;
    internal static readonly (string Function, int Arity)[] NativeSignatures =
    [
        ("counters_new", 1),
        ("counters_get", 2),
        ("counters_add", 3),
        ("counters_put", 3),
        ("counters_info", 1)
    ];
}
