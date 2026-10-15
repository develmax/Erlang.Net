namespace DistributionPayloadExitObservations;

internal static class PayloadExitNames
{
    internal const string Module = "distribution_payload_exit_probe";
    internal const string Source = "distribution_payload_exit_probe.erl";
    internal const string Plan = "source-plan.json";
    internal const string Report = "distribution-payload-exit-observations.json";
    internal const string SourceSha256 = "72055C749042B81B628CA0B3C31FABC1EC4E71CC13980106278C8450485BD8C1";
    internal const string PlanSha256 = "65588F2D47D8D6C657B9AB7D531ECF057465EB3382A142B931A6E6BDFE0FFD2D";
    internal const string ProcessError = "process-error.txt";
    internal const string ErrorSidecarFailure = "Error sidecar could not be written: ";
    internal const int NativeExecutions = 11;
    internal const string Usage = "Expected pinned OTP root, fresh output path and source-build evidence path";
    internal const string Stage = "stage.etf";
    internal const string Outer = "outer.etf";
    internal static readonly string[] Scenarios = ["linked_trap", "linked_trace_trap", "explicit_normal", "explicit_trace_trap", "explicit_kill", "dead_link_truncated", "dead_explicit_truncated", "unlinked_live_truncated", "linked_live_truncated", "explicit_live_truncated", "trailing_reason"];
    internal static readonly string[] RequiredArtifacts = ["outer.etf", "stage.etf", "setup.etf", "receiver-ready.etf", "link-input-result.etf", "links-before.etf", "receiver-before.etf", "input-control.etf", "input.packet", "input-result.etf", "selected.etf", "receiver-after.etf", "links-after.etf", "controller-after.etf", "stats-after.etf", "identities.etf", "receiver-cleanup.etf", "controller-cleanup.etf"];
}
