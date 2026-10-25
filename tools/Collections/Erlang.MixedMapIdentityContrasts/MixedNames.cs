namespace MixedMapIdentityContrasts;

internal static class MixedNames
{
    internal const int Arguments = 3;
    internal const string Usage = "Usage: Erlang.MixedMapIdentityContrasts otpRoot freshOutput sourceBuildEvidence.json";
    internal const string Module = "mixed_map_identity_probe";
    internal const string Source = "mixed_map_identity_probe.erl";
    internal const string SourceSha256 = "AE12BC7EB02B1EE17EDB445C8DD38D08A3B59ACE208743B249A2B13400CACE1F";
    internal const string Scenario = "mixed-local-external-map-start-stop";
    internal const string Report = "mixed-map-identity.json";
    internal const string RequiredArtifactsField = "RequiredArtifacts";
    internal const string CompleteStage = "complete";
    internal const string Stage = "stage";
    internal const string OutputExists = "The mixed map observation output must not exist";
    internal const string InvalidPlan = "The mixed map source plan differs from its frozen declarations";
    internal const string CaptureIncomplete = "The mixed map observation did not complete its declared raw capture";
    internal const string ExceptionFile = "exception.etf";
    internal const int NativeExecutions = 1;
    internal static readonly string[] RequiredArtifacts =
    [
        "inputs.etf",
        "started-operation.etf",
        "stopped-operation.etf",
        "cleanup-kernel.etf",
        "stage",
        "before-reference-identifiers.etf",
        "before-reference-retained-map.etf",
        "before-reference-rebuilt-map.etf",
        "before-pid-identifiers.etf",
        "before-pid-retained-map.etf",
        "before-pid-rebuilt-map.etf",
        "started-reference-identifiers.etf",
        "started-reference-retained-map.etf",
        "started-reference-rebuilt-map.etf",
        "started-pid-identifiers.etf",
        "started-pid-retained-map.etf",
        "started-pid-rebuilt-map.etf",
        "stopped-reference-identifiers.etf",
        "stopped-reference-retained-map.etf",
        "stopped-reference-rebuilt-map.etf",
        "stopped-pid-identifiers.etf",
        "stopped-pid-retained-map.etf",
        "stopped-pid-rebuilt-map.etf",
        "before-combined-retained.etf",
        "before-combined-rebuilt.etf",
        "before-facts.etf",
        "started-combined-retained.etf",
        "started-combined-rebuilt.etf",
        "started-facts.etf",
        "stopped-combined-retained.etf",
        "stopped-combined-rebuilt.etf",
        "stopped-facts.etf",
    ];
}
