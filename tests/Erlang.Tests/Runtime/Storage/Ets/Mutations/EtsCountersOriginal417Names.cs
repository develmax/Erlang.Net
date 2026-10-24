namespace Erlang.Tests;

internal static class EtsCountersOriginal417Names
{
    internal const string Test = "oracle/compiled-module/ets-source-counters417-all288-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.EtsCountersOriginal417.", Manifest = "manifest.json";
    internal const string Ets = "ets", Runtime = "erlang", Lists = "lists", File = "prim_file";
    internal const string Module = "beam_ets_counter417", FacetBeam = "ets.beam", WorkerBeam = "beam_ets_counter417.beam", ListsBeam = "lists.beam";
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library417-ets-counters-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original ETS source-derived counter cohort differs; inspect full raw/typed observations";
    internal const int Rows = 48, WholeRows = 288, CapturedNatives = 31, PublicExports = 36;
    internal static readonly (string Function, int Arity)[] NativeSignatures =
    [
        ("new", 2),
        ("whereis", 1),
        ("insert", 2),
        ("lookup", 2),
        ("member", 2),
        ("delete", 1),
        ("delete", 2),
        ("select", 2),
        ("lookup_element", 3),
        ("lookup_element", 4),
        ("match", 2),
        ("internal_select_delete", 2),
        ("internal_delete_all", 2),
        ("match_object", 2),
        ("select_count", 2),
        ("select_reverse", 2),
        ("insert_new", 2),
        ("take", 2),
        ("delete_object", 2),
        ("update_element", 3),
        ("update_element", 4),
        ("first", 1),
        ("last", 1),
        ("next", 2),
        ("prev", 2),
        ("first_lookup", 1),
        ("last_lookup", 1),
        ("next_lookup", 2),
        ("prev_lookup", 2),
        ("update_counter", 3),
        ("update_counter", 4)
    ];
}
