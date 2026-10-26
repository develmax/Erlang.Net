namespace Erlang;

internal static class BeamLiteralWire
{
    public const string Chunk = "LitT";
    public const int UncompressedTableMarker = 0;
    public const int LiteralEntryBytes = 16;
    public const int EntrySizeFieldBytes = 4;
    public const int ExternalTermTagOffset = 1;
    public const byte CompressedExternalTermTag = 80;
}
