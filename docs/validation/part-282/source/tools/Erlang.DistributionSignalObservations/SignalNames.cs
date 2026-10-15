namespace DistributionSignalObservations;

internal static class SignalNames
{
    internal const string Module = "distribution_signal_observer";
    internal const string Source = "distribution_signal_observer.erl";
    internal const string Plan = "source-plan.json";
    internal const string Report = "distribution-signal-observations.json";
    internal const string SourceSha256 = "1F509AB56D7DF09C97736B32321CD3493B3854577681E95B8778D7EE2F9BCEE3";
    internal const string PlanSha256 = "1F64FD20C0DB0BCF45388991FA29A21CD13521C533EE9F680DF59577D424EB47";
    internal const string ProcessError = "process-error.txt";
    internal const string ErrorSidecarFailure = "Error sidecar could not be written: ";
    internal const int NativeExecutions = 5;
    internal const string Usage = "Expected pinned OTP root, fresh output path and source-build evidence path";
    internal static readonly string[] Scenarios =
    [
        "monitors_both",
        "monitors_none",
        "monitors_pid",
        "monitors_name",
        "group_leader"
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
