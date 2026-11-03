namespace Erlang.NifNativeBackend;

public static class MicrostateSourceTailCaseNames
{
    public const string SuccessfulPhasesCase = "runtime/statistics-msacc-source-tail/same-physical-two-phase-helper";
    public const string WithheldCloseCase = "runtime/statistics-msacc-source-tail/withheld-close";
    public const string RunningCloseCase = "runtime/statistics-msacc-source-tail/running-first-close";
    public const string FirstFailureCase = "runtime/statistics-msacc-source-tail/first-body-failure";
    public const string FinishFailureCase = "runtime/statistics-msacc-source-tail/finish-body-failure";
    internal const int NormalWorkers = 2;
    internal const int CleanupCapacity = 5;
    internal const int GatherArity = 2;
    internal const string SuccessfulPhases = "actual-local-source-phases";
    internal const string WithheldClose = "withheld-close";
    internal const string RunningClose = "running-first-close";
    internal const string FirstFailure = "first-body-failure";
    internal const string FinishFailure = "finish-body-failure";
    internal const string Dispatcher = "actorDispatcher";
    internal const string Gate = "gate";
    internal const string Registry = "sourceTails1149";
    internal const string TrapUses = "statisticsTrapUses";
    internal const string Contract = "Actual source-tail lifecycle contract failed.";
    internal const string Sentinel = "Actual source-tail body failure witness.";
    internal const string Scope = "Actual dispatcher/source/batch/trap failure and close routes only; no public parent IPC or statistics parity.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
