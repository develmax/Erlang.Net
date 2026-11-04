internal static class EtsValidationMessages
{
    internal const string ExportMismatch = "ETS export surface differs: missing=";
    internal const string ExtraExports = "; extra=";
    internal const string FileServerPrefix = "File server: ";
    internal const string InventoryMismatch = "ETS public inventory differs from pinned exports";
    internal const string NativeMismatch = "ETS native capability surface differs from the pinned stubs";
    internal const string OnLoadFailed = "ETS dependency on_load failed: ";
    internal const string OwnerMismatch = "ETS selected backend owner mismatch";
    internal const string PreparationPrefix = "Prepare: ";
    internal const string PublicationFailed = "ETS dependency publication failed";
    internal const string ScenarioPrefix = "Scenario: ";
    internal const string Summary = "{0}: {1}/{2} contracts, {3} effects, {4} differences";
    internal const string SupervisorPrefix = "Kernel test supervisor: ";
    internal const string Usage = "Usage: Erlang.EtsCompatibility <repository> <oracle-directory> Managed|Beam";
}
