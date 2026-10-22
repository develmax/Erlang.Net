namespace Erlang.BeamBuild;

internal sealed record BeamBuildOutput(
    string Module,
    string SourcePath,
    string SourceSha256,
    string ConfigurationSha256,
    string OutputSha256,
    BeamEppNativeEvidence? NativeEvidence = null
);
