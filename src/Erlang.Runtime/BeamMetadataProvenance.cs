namespace Erlang;

public sealed record BeamMetadataProvenance(bool ChunkPresent, int PayloadBytes, int DecodedBytes)
{
    public int IgnoredTrailingBytes => PayloadBytes - DecodedBytes;
}
