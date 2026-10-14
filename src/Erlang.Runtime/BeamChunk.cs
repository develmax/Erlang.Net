namespace Erlang;

internal sealed record BeamChunk(
    string Id,
    int HeaderOffset,
    ReadOnlyMemory<byte> Payload,
    int PaddingBytes
);
