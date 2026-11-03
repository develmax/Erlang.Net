namespace Erlang.Validation.AsyncMicrostates;

internal static class AsyncObservationProgramNames
{
    internal const string UsageMessage = "Expected guarded inputs.json and full observation report path";
    internal const string UnavailableCasesProperty = "UnavailableCases";
    internal const string ConfigurationProperty = "Configuration";
    internal const string ObserverAssemblyPathProperty = "ObserverAssemblyPath";
    internal const string ExecutablePathFailureMessage = "Executing observer is not the admitted actual built DLL";
    internal const string StdlibAssemblyName = "Erlang.Otp.Stdlib";
    internal const string ConfigurationFailureMessage = "Actual runtime/package/tool configuration differs from requested profile";
    internal const string GuardsProperty = "Guards";
    internal const string PathProperty = "Path";
    internal const string Sha256Property = "Sha256";
    internal const string InputChangedPrefix = "Actual source/provider/package input changed: ";
    internal const string AssemblyGuardsProperty = "AssemblyGuards";
    internal const string AssemblyFileNameProperty = "Name";
    internal const string AssemblyHashFailureMessage = "Observation loaded assembly differs from actual child package";
    internal const string DependencyFailurePrefix = "Required prerequisite blocked ";
    internal const string DependencyReasonSeparator = ": ";
    internal const string SkippedCasePrefix = "SKIP dependency ";
    internal const string CaseStartPrefix = "START ";
    internal const string CaseDonePrefix = "DONE ";
    internal const string OriginalSingleWorkerCase = "original-driver-A1";
    internal const string OriginalTwoWorkerCase = "original-driver-A2";
    internal const string PublicCancelCase = "public-queued-ack-cancel";
    internal const string PublicCloseCase = "public-queued-ack-close";
    internal const string ComponentChildExitCase = "component-child-exit";
    internal const string ComponentWrongEpochCase = "component-wrong-epoch";
    internal const string Scope = "Windows real child async predicates/full raw observations and strict physical negative fences; no raw topology/timing normalization or whole statistics acceptance";
    internal const int ArgumentCount = 2;
    internal const int InputsArgumentIndex = 0;
    internal const int ReportArgumentIndex = 1;
}
