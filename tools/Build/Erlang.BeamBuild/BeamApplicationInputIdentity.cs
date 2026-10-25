namespace Erlang.BeamBuild;

internal sealed record BeamApplicationInputIdentity(
    string Kind,
    string SourceLogicalPath,
    string OutputPath,
    int Size,
    string Sha256
);
