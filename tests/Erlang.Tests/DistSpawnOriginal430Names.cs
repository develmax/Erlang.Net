namespace Erlang.Tests;

internal static class DistSpawnOriginal430Names
{
    internal const string Test = "oracle/compiled-module/dist-spawn430-all84-full-source-facet1-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.DistSpawnOriginal430.", Manifest = "manifest.json";
    internal const string Runtime = "erlang", File = "prim_file", Internal = "erts_internal", Function = "dist_spawn_request";
    internal const string Module = "beam_dist_spawn430", WorkerBeam = "beam_dist_spawn430.beam";
    internal const string FacetBeam = "erts_internal.beam";
    internal static readonly BeamFunctionSignature[] NativeSignatures =
    [new("dist_spawn_request", 4)];
    internal const int NativeCount = 1, FacetExports = 3;
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library430-dist-spawn-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original distribution spawn cohort differs; inspect complete raw and typed outcomes";
    internal const int Rows = 14, WholeRows = 84;
}
