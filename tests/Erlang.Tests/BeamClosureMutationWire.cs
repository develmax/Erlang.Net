namespace Erlang.Tests;

internal static class BeamClosureMutationWire
{
    internal const int FormHeaderBytes = 12;
    internal const int ChunkHeaderBytes = 8;
    internal const int LengthFieldOffset = 4;
    internal const int IdentifierBytes = 4;
    internal const int AlignmentMask = 3;
    internal const int AddedAlignedBytes = 4;
    internal const string LambdaChunk = "FunT";
}
