namespace DistributionQueueContrasts;

internal static class QueueNames
{
    internal const string BusyOption = "+zdbbl";
    internal const string BusyLimit = "1";
    internal const string SchedulersOption = "+S";
    internal const string Schedulers = "2:2";
    internal const string Launch = "native-launch.json";
    internal const int ArgumentCount = 3;
    internal const int FreshScenarioVms = 3;
    internal const string Usage = "Usage: Erlang.DistributionPressureObservations otpRoot freshOutput sourceBuildEvidence.json";
    internal const string SourceExtension = ".erl";
    internal const string PlanSha256 = "09401EC59927DE11897F595930FCECC88DF7D24D6134EB11099535989A1B1C3E";
    internal const string Sources = "Sources";
    internal const string File = "File";
    internal const string Sha256 = "Sha256";
    internal const string Required = "RequiredArtifacts";
    internal const string ExceptionFile = "exception.etf";
    internal const string CollectorExceptionFile = "collector-capture-exception.txt";
    internal const string EvaluationFormat = "try {0}:run({1},{2}),halt(0) catch C:R:S->ok=file:write_file({3},term_to_binary({{C,R,S}})),halt(1) end.";
    internal const string Stage = "stage";
    internal const string Complete = "complete";
    internal const string Report = "distribution-pressure.json";
    internal const string OutputExists = "Original distribution queue output must not exist.";
    internal const string InvalidSource = "Declared distribution queue source closure differs from frozen inputs.";
    internal const string CaptureFailure = "Original distribution queue raw observation capture is incomplete.";

    internal static readonly string[] Scenarios = ["same_controller_replies", "producer_death_then_writer", "terminal_public_free"];
    internal static readonly Dictionary<string, string> SourceHashes = new()
    {
        ["distribution_pressure_probe.erl"] = "147656D96BA59A5C527056794D2609B15D279AF23C1F1CCCD5A6E1EA2F740716",
        ["expected-policy.json"] = "DEC8C9901FE856EFC1903EB098F94BA11F02474D5B802608FA46B076B4A6F712",
    };
    internal static readonly Dictionary<string, string> ScenarioModules = new()
    {
        ["same_controller_replies"] = "distribution_pressure_probe",
        ["producer_death_then_writer"] = "distribution_pressure_probe",
        ["terminal_public_free"] = "distribution_pressure_probe",
    };
    internal static readonly Dictionary<string, string[]> RequiredArtifacts = new()
    {
        ["same_controller_replies"] = ["setup.etf", "fill-payload.etf", "before-fill.etf", "busy-barrier.etf", "after-drain.etf", "outer.etf", "cleanup.etf", "stage", "identities.etf", "missing_link-control.etf", "missing_link.packet", "missing_link-put.etf", "unlink_id-control.etf", "unlink_id.packet", "unlink_id-put.etf", "before-any-drain.etf", "expected-controls.etf", "required_replies-drain.etf", "native-launch.json"],
        ["producer_death_then_writer"] = ["setup.etf", "fill-payload.etf", "before-fill.etf", "busy-barrier.etf", "after-drain.etf", "outer.etf", "cleanup.etf", "stage", "producer-suspended.etf", "producer-down-before-drain.etf", "next-writer-suspended.etf", "next-writer-sent.etf", "release_capacity-drain.etf", "after_writer_return-drain.etf", "native-launch.json"],
        ["terminal_public_free"] = ["setup.etf", "fill-payload.etf", "before-fill.etf", "busy-barrier.etf", "after-drain.etf", "outer.etf", "cleanup.etf", "stage", "live_link-control.etf", "live_link.packet", "live_link-put.etf", "established-link.etf", "public-free-before-drain.etf", "local-down-before-drain.etf", "expected-controls.etf", "terminal_exit-drain.etf", "native-launch.json"],
    };
}
