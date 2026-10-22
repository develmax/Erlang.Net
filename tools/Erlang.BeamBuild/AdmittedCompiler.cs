namespace Erlang.BeamBuild;

internal sealed record AdmittedCompiler(
    string Executable,
    string Sha256,
    string EvidenceSha256,
    string ProviderEvidenceSha256,
    string InstalledRoot,
    string ExecutionManifestPath,
    string ExecutionManifestSha256,
    string ProvenanceKind
);
