namespace Erlang.Tests;

internal static class InternalProcessOriginal425Names
{
    internal const string Test = "oracle/compiled-module/internal-process425-full150-typed-raw";
    internal const string Prefix = "Erlang.Tests.InternalProcessOriginal425.", Manifest = "manifest.json";
    internal const string Runtime = "erlang", File = "file", Internal = "erts_internal";
    internal const string Module = "beam_internal_process425", WorkerBeam = "beam_internal_process425.beam", FacetBeam = "erts_internal.beam";
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library425-internal-process-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original internal process cohort differs; inspect complete raw and typed outcomes";
    internal const int Rows = 25, WholeRows = 150, NativeCount = 7, FacetExports = 9;
    internal static readonly BeamFunctionSignature[] NativeSignatures =
    [
        new("group_leader", 2),
        new("group_leader", 3),
        new("process_display", 2),
        new("process_flag", 3),
        new("suspend_process", 2),
        new("system_monitor", 1),
        new("system_monitor", 3)
    ];
}
