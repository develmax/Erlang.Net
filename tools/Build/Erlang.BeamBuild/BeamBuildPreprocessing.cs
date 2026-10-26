namespace Erlang.BeamBuild;

internal sealed record BeamBuildPreprocessing(
    string Policy,
    string ClosureSha256,
    string RuntimeSha256,
    string DriverSourceSha256,
    string DriverBeamSha256,
    BeamDeclaredFileIdentity[] Inputs,
    BeamDeclaredIncludeEdge[] Includes
);
