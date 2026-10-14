namespace BeamFixtureExport;

internal static class BeamFixtureWire
{
    public const int FormHeaderBytes = 12;
    public const int IdentifierBytes = 4;
    public const int FormLengthOffset = 4;
    public const int FormLengthBaseBytes = 8;
    public const int FormTypeOffset = 8;
    public const int ChunkHeaderBytes = 8;
    public const int ChunkLengthOffset = 4;
    public const int AlignmentMask = 3;

    public static ReadOnlySpan<byte> FormMarker => "FOR1"u8;

    public static ReadOnlySpan<byte> BeamMarker => "BEAM"u8;
}
