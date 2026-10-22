namespace Erlang;

internal static class LibrarySelectionNames
{
    internal static readonly string[] ManagedModules =
    [
        ErlangModuleNames.Module,
        ListModuleNames.Module,
        QueueModuleNames.Module,
        ProplistModuleNames.Module,
        OrdsetCoreNames.Module,
        MapModuleNames.Module,
        BinaryModuleNames.Module
    ];
    internal const string Request = "Invalid startup library selection";
    internal const string BeamOnly = "Selected library requires the original BEAM backend";
    internal const string Phase = "Startup library selection failed";
    internal const string Cleanup = "Startup selection and owned runtime cleanup failed";
    internal const string Primitive = "Required native primitive is unavailable";
    internal const string MissingExport = "Selected BEAM image omits a managed library export";
    internal const string Dependency = "Pure dependency must be explicitly selected with the required MFA";
    internal const string Owner = "Selected library ownership changed during startup";
    internal const string Publish = "Atomic library publication was refused";
    internal const string Purge = "Bootstrap library old code was not purged";
    internal const string Worker = "Startup worker exited without a successful result";
    internal const string OnLoadOrder = "Explicit startup on_load order must contain every selected callback exactly once";
    internal const string OnLoadFailed = "Startup on_load callback did not publish its original image";
    internal const string ContextSeparator = ": ";
    internal const string DependencyArrow = " -> ";
    internal const string FunctionSeparator = ":";
    internal const string AritySeparator = "/";
}
