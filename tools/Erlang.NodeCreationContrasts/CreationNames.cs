namespace CreationBoundaryContrasts;

internal static class CreationNames
{
    internal const int ArgumentCount = 3;
    internal const int FreshScenarioVms = 6;
    internal const string Usage = "Usage: Erlang.NodeCreationContrasts otpRoot freshOutput sourceBuildEvidence.json";
    internal const string Module = "creation_boundary_probe";
    internal const string Source = "creation_boundary_probe.erl";
    internal const string PlanSha256 = "BF8AF703AE81CD829FFFF55A910DBF51B70F594EFF70445EBE5DCDE8B6ECA481";
    internal const string SourceSha256 = "11CE68B484E4DDC6AB0C2A4C66706D3699C6E8B2EDDB2B25547C701649199AAF";
    internal const string Sources = "Sources";
    internal const string File = "File";
    internal const string Sha256 = "Sha256";
    internal const string Required = "RequiredArtifacts";
    internal const string ExceptionFile = "exception.etf";
    internal const string CollectorExceptionFile = "collector-capture-exception.txt";
    internal const string EvaluationFormat = "try {0}:run({1},{2}),halt(0) catch C:R:S->ok=file:write_file({3},term_to_binary({{C,R,S}})),halt(1) end.";
    internal const string Stage = "stage";
    internal const string Complete = "complete";
    internal const string Report = "creation-boundary.json";
    internal const string OutputExists = "Original creation boundary output must not exist.";
    internal const string InvalidSource = "Declared creation boundary source closure differs from frozen inputs.";
    internal const string CaptureFailure = "Original creation boundary raw observation capture is incomplete.";
    internal static readonly string[] Scenarios = ["truncated_zero", "truncated_one", "maximum_small", "first_big", "retained_four", "retained_max_wrap"];
    internal static readonly Dictionary<string, string> SourceHashes = new()
    {
        [Source] = SourceSha256
    };
    internal static readonly string[] ArtifactFiles = ["inputs.etf", "runtime-profile.etf", "before.etf", "retained-before.etf", "call-outcome.etf", "active.etf", "owner-after.etf", "retained-after.etf", "kernel-down.etf", "inactive.etf", "stage"];
}
