namespace DistributionQueueContrasts;

internal static class QueueNames
{
    internal const int ArgumentCount = 3;
    internal const int FreshScenarioVms = 7;
    internal const string Usage = "Usage: Erlang.DistributionQueueContrasts otpRoot freshOutput sourceBuildEvidence.json";
    internal const string SourceExtension = ".erl";
    internal const string PlanSha256 = "7330C72086B9E1522438BECED9E9A4FB17AAA172F1DBBE4727C9C7910E443726";
    internal const string Sources = "Sources";
    internal const string File = "File";
    internal const string Sha256 = "Sha256";
    internal const string Required = "RequiredArtifacts";
    internal const string ExceptionFile = "exception.etf";
    internal const string CollectorExceptionFile = "collector-capture-exception.txt";
    internal const string EvaluationFormat = "try {0}:run({1},{2}),halt(0) catch C:R:S->ok=file:write_file({3},term_to_binary({{C,R,S}})),halt(1) end.";
    internal const string Stage = "stage";
    internal const string Complete = "complete";
    internal const string Report = "distribution-queue.json";
    internal const string OutputExists = "Original distribution queue output must not exist.";
    internal const string InvalidSource = "Declared distribution queue source closure differs from frozen inputs.";
    internal const string CaptureFailure = "Original distribution queue raw observation capture is incomplete.";
    internal static readonly string[] Scenarios = ["small", "large_binary", "two_binaries", "two_buffers", "handle_domain", "input_counter", "input_malformed"];
    internal static readonly Dictionary<string, string> SourceHashes = new()
    {
        ["additive-source-plan.json"] = "DABEF167F52C25FE880C84D743635D65962D42CCFDDB8B9E4766BF652AF30FC1",
        ["distribution_queue_stat_additive_probe.erl"] = "C3CCADF2350B4D98CAEE03A16D177551807FEABDE35309C59F03A46F75A72A0C",
        ["distribution_queue_stat_fresh_probe.erl"] = "E45AD946E31E8CD2D10A685AC651CAEE575268834A8B37A4473AAAAD8C3999BF",
        ["distribution_queue_stat_probe.erl"] = "D2C1DB014761E431C8B463AEEDF2E21DEB484C93BDE912E2ED739C4FB5E7FB53",
        ["original-source-plan.json"] = "A2D0238AA5AB4827723CC7E8D4723407A168464C949069D7C51886A9F678CAC6"
    };
    internal static readonly Dictionary<string, string> ScenarioModules = new()
    {
        ["small"] = "distribution_queue_stat_fresh_probe",
        ["large_binary"] = "distribution_queue_stat_fresh_probe",
        ["two_binaries"] = "distribution_queue_stat_fresh_probe",
        ["two_buffers"] = "distribution_queue_stat_additive_probe",
        ["handle_domain"] = "distribution_queue_stat_additive_probe",
        ["input_counter"] = "distribution_queue_stat_additive_probe",
        ["input_malformed"] = "distribution_queue_stat_additive_probe"
    };
    internal static readonly Dictionary<string, string[]> RequiredArtifacts = new()
    {
        ["small"] = ["pending.etf", "handle.etf", "kernel-notice.etf", "runtime.etf", "cleanup.etf", "stage", "case.etf", "small-payload.etf", "small-empty-stat.etf", "small-queued-stat.etf", "small-previous-option.etf", "small-raw-data.etf", "small-after-stat.etf", "small-wire-shape.etf", "small-control.etf", "small.packet"],
        ["large_binary"] = ["pending.etf", "handle.etf", "kernel-notice.etf", "runtime.etf", "cleanup.etf", "stage", "case.etf", "large_binary-payload.etf", "large_binary-empty-stat.etf", "large_binary-queued-stat.etf", "large_binary-previous-option.etf", "large_binary-raw-data.etf", "large_binary-after-stat.etf", "large_binary-wire-shape.etf", "large_binary-control.etf", "large_binary.packet"],
        ["two_binaries"] = ["pending.etf", "handle.etf", "kernel-notice.etf", "runtime.etf", "cleanup.etf", "stage", "case.etf", "two_binaries-payload.etf", "two_binaries-empty-stat.etf", "two_binaries-queued-stat.etf", "two_binaries-previous-option.etf", "two_binaries-raw-data.etf", "two_binaries-after-stat.etf", "two_binaries-wire-shape.etf", "two_binaries-control.etf", "two_binaries.packet"],
        ["two_buffers"] = ["case.etf", "profile.etf", "pending.etf", "pending-stat.etf", "handle.etf", "first-kernel-notice.etf", "cleanup.etf", "stage", "first-payload.etf", "second-payload.etf", "before-stat.etf", "both-queued-stat.etf", "after-first-owner-stat.etf", "after-second-owner-stat.etf", "empty-previous-option.etf", "empty-data.etf", "empty-controller-stat.etf", "after-empty-owner-stat.etf", "first-previous-option.etf", "first-raw-data.etf", "first-controller-stat.etf", "first-wire-shape.etf", "first.packet", "first-control.etf", "second-previous-option.etf", "second-raw-data.etf", "second-controller-stat.etf", "second-wire-shape.etf", "second.packet", "second-control.etf"],
        ["handle_domain"] = ["case.etf", "profile.etf", "pending.etf", "pending-stat.etf", "handle.etf", "first-kernel-notice.etf", "cleanup.etf", "stage", "nil-stat.etf", "atom-stat.etf", "pid-stat.etf", "owner-stat.etf", "controller-stat.etf", "third-stat.etf", "third-cleanup.etf", "closed-reason.etf", "old-after-close-stat.etf", "reopened-pending.etf", "reopened-handle.etf", "reopened-kernel-notice.etf", "old-after-reopen-stat.etf", "new-after-reopen-stat.etf"],
        ["input_counter"] = ["case.etf", "profile.etf", "pending.etf", "pending-stat.etf", "handle.etf", "first-kernel-notice.etf", "cleanup.etf", "stage", "before-input-owner-stat.etf", "input-controller-before-stat.etf", "input-put-outcome.etf", "input-controller-after-stat.etf", "after-input-owner-stat.etf", "input-payload.etf", "input.packet", "delivery.etf", "after-input-controller-stat.etf"],
        ["input_malformed"] = ["case.etf", "profile.etf", "pending.etf", "pending-stat.etf", "handle.etf", "first-kernel-notice.etf", "cleanup.etf", "stage", "before-input-owner-stat.etf", "input-controller-before-stat.etf", "input-put-outcome.etf", "input-controller-after-stat.etf", "after-input-owner-stat.etf", "malformed.packet", "malformed-controller-down.etf"]
    };
}
