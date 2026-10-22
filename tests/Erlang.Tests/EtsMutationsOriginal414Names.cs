namespace Erlang.Tests;

internal static class EtsMutationsOriginal414Names
{
    internal const string Test = "oracle/compiled-module/ets-source-mutations414-all252-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.EtsMutationsOriginal414.", Manifest = "manifest.json";
    internal const string Ets = "ets", Runtime = "erlang", Lists = "lists", File = "prim_file";
    internal const string Module = "beam_ets_mutations414", FacetBeam = "ets.beam", WorkerBeam = "beam_ets_mutations414.beam", ListsBeam = "lists.beam";
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library414-ets-mutations-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original ETS source-derived mutation cohort differs; inspect full raw/typed observations";
    internal const int Rows = 42, WholeRows = 252, CapturedNatives = 21, PublicExports = 26;
    internal static readonly (string Function, int Arity)[] NativeSignatures =
    [
        ("new",2),("whereis",1),("insert",2),("lookup",2),("member",2),("delete",1),("delete",2),
        ("select",2),("lookup_element",3),("lookup_element",4),("match",2),
        ("internal_select_delete",2),("internal_delete_all",2),
        ("match_object",2),("select_count",2),("select_reverse",2),
        ("insert_new",2),("take",2),("delete_object",2),("update_element",3),("update_element",4)
    ];
}
