namespace Erlang.BeamBuild;

internal sealed record BeamApplicationOutput(
    string Path,
    string TargetPath,
    string Kind,
    string Sha256,
    long Bytes
);
