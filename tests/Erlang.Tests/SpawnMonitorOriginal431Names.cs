namespace Erlang.Tests;

internal static class SpawnMonitorOriginal431Names
{
    internal const string Test = "oracle/compiled-module/spawn-monitor431-all42-full-source-facet4-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.SpawnMonitorOriginal431.", Manifest = "manifest.json";
    internal const string Runtime = "erlang", File = "prim_file", Internal = "erts_internal", Function = "dist_spawn_request";
    internal const string Module = "beam_spawn_monitor431", WorkerBeam = "beam_spawn_monitor431.beam";
    internal const string FacetBeam = "erts_internal.beam";
    internal static readonly BeamFunctionSignature[] NativeSignatures =
    [new("get_dflags", 0), new("new_connection", 1), new("create_dist_channel", 3), new("dist_spawn_request", 4)];
    internal const int NativeCount = 4, FacetExports = 6;
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library431-spawn-monitor-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original distribution spawn cohort differs; inspect complete raw and typed outcomes";
    internal const int Rows = 7, WholeRows = 42;
}
