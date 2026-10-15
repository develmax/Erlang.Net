namespace DistributionTraceObservations;

internal static class TraceNames
{
    internal const string Module = "distribution_trace_probe";
    internal const string Source = "distribution_trace_probe.erl";
    internal const string Plan = "source-plan.json";
    internal const string Report = "distribution-trace-observations.json";
    internal const string SourceSha256 = "30FD1EA415E99155641E747F51E3381C0573C9DCFD0DDEFD880612ABCC36429D";
    internal const string PlanSha256 = "3C79D54DB6A539ECBC5B81925FEAB43443587DD178203BF1F955BDD100648AC1";
    internal const string ProcessError = "process-error.txt";
    internal const string ErrorSidecarFailure = "Error sidecar could not be written: ";
    internal const int NativeExecutions = 8;
    internal const string Usage = "Expected pinned OTP root, fresh output path and source-build evidence path";
    internal const string Stage = "stage.etf";
    internal const string Outer = "outer.etf";
    internal static readonly string[] Scenarios = ["send", "registered", "nil", "undefined", "wide_flags", "exit_link", "exit2_trap", "exit2_normal"];
    internal static readonly string[] RequiredArtifacts = ["stage.etf", "outer.etf", "setup.etf", "receiver-ready.etf", "links-before.etf", "token-before.etf", "input.etf", "input.packet", "input-result.etf", "queued.etf", "ignored-exit-state.etf", "completion-input.etf", "selected.etf", "tracer-mailbox.etf", "receiver-identities.etf", "receiver-cleanup.etf", "controller-cleanup.etf", "tracer-setting.etf", "tracer-cleanup.etf"];
}
