namespace Erlang.NifNativeBackend;

public static class MicrostateControlCaseNames
{
    public const string SameActor = "runtime/statistics-msacc-capacity-one-same-actor-enable-work-actual-idle";
    internal const string LedgerGate = "gate";
    internal const string LedgerRecords = "records";
    internal const string NormalWorkers = "normalWorkers";
    internal const string Thread = "thread";
    internal const string Artifacts = "artifacts";
    internal const string OutputPrefix = "msacc-controls-";
    internal const string GuidFormat = "D";
    internal const string Separator = "-";
    internal const string RawExtension = ".etf";
    internal const string DeterministicExtension = "-deterministic.etf";
    internal const string PublicSample = "public-complete-sample";
    internal const string SameActorBefore = "same-actor-before";
    internal const string SameActorAfter = "same-actor-after";
    internal const string SameActorIdle = "same-actor-physical-idle";
    internal const string FullFailure = "full-erlang-failure";
    internal const string Raised = "raised";
}
