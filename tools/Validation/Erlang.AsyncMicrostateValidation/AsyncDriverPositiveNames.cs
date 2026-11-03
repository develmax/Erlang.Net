namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncDriverPositiveNames
{
    internal const string PackageManifestProperty = "PackageManifest";
    internal const string ErlangModule = "erlang";
    internal const string ErtsInternalModule = "erts_internal";
    internal const string DdllModule = "erl_ddll";
    internal const string ListsModule = "lists";
    internal const string MapsModule = "maps";
    internal const string BinaryModule = "binary";
    internal const string ObserverModule = "async_msacc1127";
    internal const string FileModule = "file";
    internal const string IoLibraryModule = "io_lib";
    internal const string StringModule = "string";
    internal const string InitModule = "init";
    internal const string IoModule = "io";
    internal const string DdllBeamProperty = "DdllBeam";
    internal const string ObserverBeamProperty = "ObserverBeam";
    internal const string PublicationFailureMessage = "Actual observer publication failed";
    internal const string SelectorAtom = "microstate_accounting";
    internal const string TypeKey = "type";
    internal const string AsyncTypeAtom = "async";
    internal const string ContractFailureMessage = "Actual child/public accounting contract failed";
    internal const string StatisticsFunction = "statistics";
    internal const string CountersKey = "counters";
    internal const string SystemFlagFunction = "system_flag";
    internal const string TrueAtom = "true";
    internal const string FalseAtom = "false";
    internal const string ResetAtom = "reset";
    internal const string LoadDriverFunction = "load_driver";
    internal const string DriverDirectoryProperty = "DriverDirectory";
    internal const string DriverName = "async_msacc1127_drv";
    internal const string OkAtom = "ok";
    internal const string ObserverRunFunction = "run";
    internal const string OriginalSingleWorkerProperty = "OriginalA1";
    internal const string OriginalTwoWorkerProperty = "OriginalA2";
    internal const string AllCasesReturnedKey = "all_cases_returned";
    internal const string FinalLiveJobsZeroKey = "final_live_jobs_zero";
    internal const string FinalOnlyControllerStateKey = "final_only_controller_state";
    internal const string UnloadDriverFunction = "unload_driver";
    internal const string PrimaryFailurePrefix = "PRIMARY ";
    internal const string MissingObservationMessage = "Missing actual driver observation";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    internal const int ObservationIntervalMilliseconds = 25;
    internal const int SingleAsyncWorkerCount = 1;
    internal const int TwoAsyncWorkerCount = 2;
}
