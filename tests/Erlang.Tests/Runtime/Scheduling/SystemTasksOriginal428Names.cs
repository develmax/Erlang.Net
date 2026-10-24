namespace Erlang.Tests;

internal static class SystemTasksOriginal428Names
{
    internal const string Test = "oracle/compiled-module/system-tasks428-all144-full-source-facet5-full-typed-raw";
    internal const string Prefix = "Erlang.Tests.SystemTasksOriginal428.", Manifest = "manifest.json";
    internal const string Runtime = "erlang", File = "prim_file", Internal = "erts_internal", HandlerModule = "erts_dirty_process_signal_handler", HandlerBeam = "erts_dirty_process_signal_handler.beam";
    internal const string Module = "beam_system_tasks428", WorkerBeam = "beam_system_tasks428.beam";
    internal const string FacetBeam = "erts_internal.beam";
    internal static readonly BeamFunctionSignature[] NativeSignatures =
    [new("garbage_collect", 1), new("request_system_task", 3), new("request_system_task", 4), new("check_dirty_process_code", 2), new("dirty_process_handle_signals", 1)];
    internal const int NativeCount = 5, FacetExports = 7;
    internal const string Run = "run", Stacks = "run_stacks", Encode = "term_to_binary";
    internal const string Values = "values.etf", StackValues = "stacks.etf", Actual = "actual-", Expected = "expected-", Json = ".json";
    internal const string Output = "artifacts/library428-system-tasks-permanent", Guid = "N", Receipt = "comparison.json";
    internal const string Failure = "Original native system-task cohort differs; inspect complete raw and typed outcomes";
    internal const int Rows = 24, WholeRows = 144;
}
