namespace Erlang.Tests;

internal static class ModuleLoadingHostDiagnostics
{
    public const string MissingCurrent = "Owned current module was not published";
    public const string FailedVisible = "Failed registration leaked module or export state";
    public const string SnapshotChanged = "Published module snapshot changed or duplicated";
    public const string PartialVisible = "Concurrent reader observed a partially published module";
    public const string DuplicateAccepted = "Duplicate module export registration was accepted";
}
