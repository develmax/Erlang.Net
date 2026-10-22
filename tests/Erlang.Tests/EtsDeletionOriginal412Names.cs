namespace Erlang.Tests;

internal static class EtsDeletionOriginal412Names
{
    internal const string Test = "oracle/compiled-module/ets-source-deletion412-all72-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.EtsDeletionOriginal412.", Manifest = "manifest.json";
    internal const string Ets = "ets", Runtime = "erlang", Lists = "lists", File = "prim_file";
    internal const string Module = "beam_ets_delete412", FacetBeam = "ets.beam", WorkerBeam = "beam_ets_delete412.beam", ListsBeam = "lists.beam";
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library412-ets-deletion-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original ETS source-derived deletion cohort differs; inspect full raw/typed observations";
    internal const int Rows = 12, WholeRows = 72, CapturedNatives = 13, PublicExports = 17;
    internal static readonly (string Function, int Arity)[] NativeSignatures =
    [
        ("new",2),("whereis",1),("insert",2),("lookup",2),("member",2),("delete",1),("delete",2),
        ("select",2),("lookup_element",3),("lookup_element",4),("match",2),
        ("internal_select_delete",2),("internal_delete_all",2)
    ];
}
