namespace Erlang;

internal static class EtfHeaderLayout
{
    public const int CompressedPayloadOffset = 6;
    public const int CompressedSizeOffset = 2;
    public const int MinimumCompressedPacketBytes = 6;
    public const int MinimumPacketBytes = 2;
    public const int PlainPayloadOffset = 1;
    public const int TagOffset = 1;
    public const int Version = 131;
    public const int VersionOffset = 0;
}
