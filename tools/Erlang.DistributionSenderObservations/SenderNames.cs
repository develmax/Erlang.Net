namespace DistributionSenderObservations;

internal static class SenderNames
{
    internal const string Module = "distribution_sender_observer";
    internal const string Source = "distribution_sender_observer.erl";
    internal const string Plan = "source-plan.json";
    internal const string Report = "distribution-sender-observations.json";
    internal const string SourceSha256 = "4CC3F5250DA5CF2BE4CD57FAB92CB862AE8B6BEDE44C3FA20468C78809BAFC91";
    internal const string PlanSha256 = "A2E8397D2A8443FE45A08F4979F9E2B380FB1753EFAA3733FE62F8A53CAB7CEA";
    internal const string ProcessError = "process-error.txt";
    internal const string ErrorSidecarFailure = "Error sidecar could not be written: ";
    internal const int NativeExecutions = 2;
    internal const string Usage = "Expected pinned OTP root, fresh output path and source-build evidence path";
    internal static readonly string[] Scenarios =
    [
        "sender_plain",
        "sender_trace"
    ];
    internal static readonly string[] RequiredArtifacts =
    [
        "identities.etf",
        "operations.etf",
        "outputs.etf",
        "cleanup.etf",
        "stage"
    ];
}
