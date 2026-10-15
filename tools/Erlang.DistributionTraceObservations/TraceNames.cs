namespace DistributionTraceObservations;

internal static class TraceNames
{
    internal const string Module = "distribution_trace_probe";
    internal const string Source = "distribution_trace_probe.erl";
    internal const string Plan = "source-plan.json";
    internal const string Report = "distribution-trace-observations.json";
    internal const string SourceSha256 = "98A6A5ED875B0692A781350BF8E7C73A6A7FFDD2F76DA92E2D0FF8430CA17D0E";
    internal const string PlanSha256 = "8F549183B3321E21FEE4943DAD60FDC39B9FBC65F83E0A96F6A987B33E20BD5B";
    internal const string ProcessError = "process-error.txt";
    internal const string ErrorSidecarFailure = "Error sidecar could not be written: ";
    internal const int NativeExecutions = 8;
    internal const string Usage = "Expected pinned OTP root, fresh output path and source-build evidence path";
    internal const string Stage = "stage.etf";
    internal const string Outer = "outer.etf";
    internal static readonly string[] Scenarios = ["send", "registered", "nil", "undefined", "wide_flags", "exit_link", "exit2_trap", "exit2_normal"];
    internal static readonly string[] RequiredArtifacts = ["stage.etf", "outer.etf", "setup.etf", "receiver-ready.etf", "links-before.etf", "token-before.etf", "input.etf", "input.packet", "input-result.etf", "queued.etf", "ignored-exit-state.etf", "completion-input.etf", "selected.etf", "tracer-mailbox.etf", "receiver-identities.etf", "receiver-cleanup.etf", "controller-cleanup.etf", "tracer-setting.etf", "tracer-cleanup.etf"];
}
