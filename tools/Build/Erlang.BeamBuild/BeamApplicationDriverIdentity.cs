namespace Erlang.BeamBuild;

internal sealed record BeamApplicationDriverIdentity(
    AdmittedCompiler Runtime,
    string Directory,
    string SourceSha256,
    string BeamSha256,
    string ConfigurationSha256
);
