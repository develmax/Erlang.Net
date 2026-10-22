namespace Erlang.BeamBuild;

internal sealed record BeamBuildState(
    string CompilerSha256,
    string EvidenceSha256,
    string ProviderEvidenceSha256,
    string ToolModuleVersionId,
    BeamBuildOutput[] Outputs,
    BeamBuildPreprocessing? Preprocessing = null
);
