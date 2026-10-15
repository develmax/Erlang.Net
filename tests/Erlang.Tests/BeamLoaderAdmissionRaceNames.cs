namespace Erlang.Tests;

internal static class BeamLoaderAdmissionRaceNames
{
    internal const string CodeModification = "CodeModification";
    internal const string Enter = "Enter";
    internal const string PendingWriters = "PendingSynchronousWriters";
    internal const string FinishReferences = "FinishPreparedReferences";
    internal const string ModuleGate = "gate";
    internal const string Entered = "entered";
    internal const string Failure = "Loader consumption and closure admission race failed";
    internal const int WatchdogSeconds = 10;
}
