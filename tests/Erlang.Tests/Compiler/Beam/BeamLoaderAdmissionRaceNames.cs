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
    internal const string RuntimeDisposed = "disposed";
    internal const string RuntimeHalt = "haltResult";
    internal const string SourceClosing = "sourceClosing";
    internal const string NifClosing = "nifClosing";
    internal const string SourceShutdown = "sourceShutdown";
    internal const string NifShutdown = "nifShutdown";
    internal const string GcClosing = "gcStatisticsClosing";
    internal const string TrapClosing = "statisticsTrapClosing";
    internal const string AdmitGc = "AdmitGcStatistics";
    internal const string AdmitTrap = "AdmitStatisticsTrap";
    internal const string TrapFunction = "await_sched_wall_time_modifications";
    internal const string AsTask = "AsTask";
    internal const string Result = "Result";
    internal const string HelperImage = "Image";
    internal const string GcPrivateImage = "gcStatisticsPrivateImage";
    internal const string TrapPrivateImage = "statisticsTrapPrivateImage";
    internal const string LiteralDomain = "LiteralDomain800";
    internal const string LiteralRetirement = "Retirement800";
    internal const string GcUses = "gcStatisticsUses";
    internal const string TrapUses = "statisticsTrapUses";
    internal const string AdmissionException = "Erlang.NifImageAdmissionException";
    internal const string AdmissionFailure = "Failure";
    internal const string RegistryReentry = "ReentrantRegistryShutdown";
    internal const string ImageDisposed = "Disposed800";
    internal const string GcPreparing = "gcStatisticsPreparing";
    internal const string GcStateGate = "gcStatisticsGate";
    internal const int WatchdogSeconds = 10;
}
