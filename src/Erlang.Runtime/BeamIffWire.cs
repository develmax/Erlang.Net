namespace Erlang;

internal static class BeamIffWire
{
    public const int IdentifierBytes = 4;
    public const int FormHeaderBytes = 8;
    public const int ChunkAlignmentMask = 3;
    public static ReadOnlySpan<byte> FormId => "FOR1"u8;
    public static ReadOnlySpan<byte> FormType => "BEAM"u8;
}
