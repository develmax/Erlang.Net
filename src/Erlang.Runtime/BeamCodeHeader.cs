namespace Erlang;

internal sealed record BeamCodeHeader(
    int Version,
    int MaximumOpcode,
    int LabelCount,
    int FunctionCount,
    ReadOnlyMemory<byte> Instructions
);
