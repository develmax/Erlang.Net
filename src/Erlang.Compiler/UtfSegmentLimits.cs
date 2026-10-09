

namespace Erlang.Compiler;

internal static class UtfSegmentLimits
{
    // Scalar limits, byte counts and UTF-16 character counts are separate concepts.
    public const int MaximumScalar = 0x10ffff;
    public const int MaximumEncodedBytes = 4;
    public const int MaximumUtf16Characters = 2;
    public const int Utf16BytesPerCharacter = 2;
    public const int Utf32BytesPerScalar = 4;
}
