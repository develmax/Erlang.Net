namespace AdmissionOracle;

internal static class AdmissionNames
{
    internal const string PlanSha256 = "8E67B33365842F0D29156AD2B44F227481CC68A800D38F46E131E9F8B5F5198E";
    internal const int ArgumentCount = 3;
    internal const string Usage = "Usage: AdmissionOracle otpRoot freshOutput sourceBuildEvidence.json";
    internal const string Sources = "Sources";
    internal const string File = "File";
    internal const string Sha256 = "Sha256";
    internal const string Required = "RequiredArtifacts";
    internal const string ExceptionFile = "exception.etf";
    internal const string EvaluationFormat = "try {0}:run({1}),halt(0) catch C:R:S->ok=file:write_file({2},term_to_binary({{C,R,S}})),halt(1) end.";
    internal const string Stage = "stage";
    internal const string Complete = "complete";
    internal const string Report = "local-node-admission.json";
    internal const string OutputExists = "Original admission output must not exist.";
    internal const string InvalidSource = "Declared admission source closure differs from frozen inputs.";
    internal const string CaptureFailure = "Original admission raw observation capture is incomplete.";
    internal static readonly string[] Scenarios = ["admission", "compiled"];
    internal static readonly string[] Modules = ["local_node_admission_probe", "setnode_compiled_driver"];
    internal const string LocalNodeAdmissionProbeSha = "602C79F6C5F117BB2891FA2975FD80B6077CD3CCD794B0FD36890194CE83033F";
    internal const string SetnodeCompiledDriverSha = "BD6DABB2021ADACCD7193C0992F03BF460390D9B234CBA4671252DE61D00F494";
    internal const string SetnodeGatewayProbeSha = "BD9C14C6D586EAB395E2D8EA9F1B3EC0824C2783BB69F5556C565B42B4F30685";
    internal static readonly Dictionary<string, string> SourceHashes = new()
    {
        ["local_node_admission_probe.erl"] = "602C79F6C5F117BB2891FA2975FD80B6077CD3CCD794B0FD36890194CE83033F",
        ["setnode_compiled_driver.erl"] = "BD6DABB2021ADACCD7193C0992F03BF460390D9B234CBA4671252DE61D00F494",
        ["setnode_gateway_probe.erl"] = "BD9C14C6D586EAB395E2D8EA9F1B3EC0824C2783BB69F5556C565B42B4F30685",
    };
    internal static readonly Dictionary<string, string[]> RequiredArtifacts = new()
    {
        ["admission"] = ["before.etf", "missing-kernel.etf", "retained-external.etf", "invalid-name.etf", "invalid-type.etf", "forbidden-nonode.etf", "negative-creation.etf", "overflow-creation.etf", "admitted.etf", "active.etf", "nodeup-first.etf", "nodeup-second.etf", "already-active.etf", "kernel-down.etf", "nodedown-first.etf", "nodedown-second.etf", "inactive.etf", "retained-after.etf", "stage"],
        ["compiled"] = ["missing.etf", "inactive.etf", "watch.etf", "started.etf", "active.etf", "already-active.etf", "nodeup.etf", "kernel-down.etf", "nodedown.etf", "stopped.etf", "cleanup.etf", "stage"],
    };
}
