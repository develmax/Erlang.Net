namespace Erlang.Tests;

internal static class MatchSpecLeafOriginal420Names
{
    internal const string Test = "oracle/compiled-module/ets-table-matchspec-leaf420-all96-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.MatchSpecLeafOriginal420.", Manifest = "manifest.json";
    internal const string Ets = "ets", Runtime = "erlang", Lists = "lists", File = "prim_file";
    internal const string Module = "beam_ms_leaf420", FacetBeam = "ets.beam", WorkerBeam = "beam_ms_leaf420.beam", ListsBeam = "lists.beam";
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library420-ms-leaf-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original ETS source-derived table match-spec nested-operation cohort differs; inspect full raw/typed observations";
    internal const int Rows = 16, WholeRows = 96, CapturedNatives = 40, PublicExports = 46;
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
        ("update_counter", 4),
        ("rename", 2),
        ("setopts", 2),
        ("give_away", 3),
        ("is_compiled_ms", 1),
        ("match_spec_compile", 1),
        ("match_spec_run_r", 3),
        ("internal_request_all", 0),
        ("safe_fixtable", 2),
        ("slot", 2)
    ];
}
