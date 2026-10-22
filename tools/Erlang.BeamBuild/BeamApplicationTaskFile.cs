namespace Erlang.BeamBuild;

internal sealed record BeamApplicationTaskFile(
    string Source,
    string TargetPath,
    string Kind,
    string Sha256,
    long Bytes
);
