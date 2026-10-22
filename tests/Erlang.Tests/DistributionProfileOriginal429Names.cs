namespace Erlang.Tests;

internal static class DistributionProfileOriginal429Names
{
    internal const string Test = "oracle/compiled-module/distribution-profile429-all48-full-source-facet1-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.DistributionProfileOriginal429.", Manifest = "manifest.json";
    internal const string Runtime = "erlang", File = "prim_file", Internal = "erts_internal", Function = "get_dflags";
    internal const string Module = "beam_distribution_profile429", WorkerBeam = "beam_distribution_profile429.beam";
    internal const string FacetBeam = "erts_internal.beam";
    internal static readonly BeamFunctionSignature[] NativeSignatures =
    [new("get_dflags", 0)];
    internal const int NativeCount = 1, FacetExports = 3;
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary", ProfileFunction = "profile_record", ProfileRecord = "profile-record.etf", Record = "erts_dflags";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library429-distribution-profile-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original distribution profile cohort differs; inspect complete raw and typed outcomes";
    internal const int Rows = 8, WholeRows = 48;
}
