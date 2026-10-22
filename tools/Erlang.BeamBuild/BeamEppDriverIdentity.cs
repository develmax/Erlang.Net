namespace Erlang.BeamBuild;

internal sealed record BeamEppDriverIdentity(
    AdmittedCompiler Runtime,
    string Directory,
    string SourceSha256,
    string BeamSha256,
    string BootstrapConfigurationSha256
);
