namespace Erlang.NifNativeBackend;

internal static class NifLinuxCallbackTraceNames
{
    internal const string DiagnosticStatus = "DIAGNOSTIC_NUMERIC_FOREIGN_GC_RETIREMENT_COMPLETED";
    internal const string ForeignBarrier = "foreign-barrier-arrived";
    internal const string CollectFirstBefore = "collect-first-before";
    internal const string CollectFirstAfter = "collect-first-after";
    internal const string FinalizersBefore = "finalizers-before";
    internal const string FinalizersAfter = "finalizers-after";
    internal const string CollectSecondBefore = "collect-second-before";
    internal const string CollectSecondAfter = "collect-second-after";
    internal const string CollectionFailed = "forced-collection-managed-failure";
    internal const string Journal = "diagnostic-stage.ndjson";
    internal const string MapModule = "map_size_callback_contract";
    internal const string Observe = "observe";
    internal const string Widths = "widths";
    internal const string WidthsTerm = "diagnostic-widths.etf";
    internal const string RuntimeCreated = "runtime-created";
    internal const string RuntimeDisposeBefore = "runtime-dispose-before";
    internal const string RuntimeDisposeAfter = "runtime-dispose-after";
    internal const string RuntimeDisposeFailed = "runtime-dispose-failed";
    internal const string BeforeCleanupFailure = "managed-exception-before-cleanup";
    internal const string InvokeBefore = "invoke-before";
    internal const string InvokeAfter = "invoke-after";
    internal const string InvokeFailed = "invoke-managed-exception";
    internal const string ForeignValidated = "foreign-results-validated";
    internal const string RetireBefore = "image-retire-before";
    internal const string RetireAfter = "image-retire-after";
    internal const string ModuleDisposeBefore = "compiled-module-dispose-before";
    internal const string ModuleDisposeAfter = "compiled-module-dispose-after";
    internal const string PreflightPrefix = "diagnostic-map-preflight-";
    internal const string PreflightSuffix = ".etf";
    internal const string GlobalFailure = "diagnostic-unhandled.json";
}
