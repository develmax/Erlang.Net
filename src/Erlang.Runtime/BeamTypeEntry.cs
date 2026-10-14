namespace Erlang;

internal sealed record BeamTypeEntry(
    int TypeUnion,
    int MetadataFlags,
    long Minimum,
    long Maximum,
    byte SizeUnit
);
