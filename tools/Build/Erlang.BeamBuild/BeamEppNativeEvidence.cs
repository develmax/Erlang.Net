namespace Erlang.BeamBuild;

internal sealed record BeamEppNativeEvidence(
    string RequestPath,
    string RequestSha256,
    string ReportPath,
    string ReportSha256,
    string OwnedInputRoot,
    BeamEppArtifactIdentity[] Artifacts
);
