

namespace Erlang.Compiler;

internal static class BitStorageLayout
{
    public const int BitsPerByte = 8;
    public const int ByteRoundingOffset = BitsPerByte - 1;
    public const int MostSignificantBitIndex = BitsPerByte - 1;
}
