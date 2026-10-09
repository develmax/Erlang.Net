namespace Erlang.Compiler;

// These names describe bit segments, not lexer token kinds or term type categories.
internal static class BitSegmentTypes
{
    public const string Integer = "integer";
    public const string Binary = "binary";
    public const string Float = "float";
    public const string Utf8 = "utf8";
    public const string Utf16 = "utf16";
    public const string Utf32 = "utf32";
}

internal static class BitByteOrders
{
    public const string Big = "big";
    public const string Little = "little";
    public const string Native = "native";
}

internal static class BitSyntaxDefaults
{
    // Default integer width and the number of bits in a byte happen to be equal.
    public const int IntegerWidth = 8;
    public const int FloatWidth = 64;
    public const int BinaryUnit = 8;
    public const int NumericUnit = 1;
}

internal static class FloatSegmentWidths
{
    public const int Half = 16;
    public const int Single = 32;
    public const int Double = 64;
}

internal static class BitStorageLayout
{
    public const int BitsPerByte = 8;
    public const int ByteRoundingOffset = BitsPerByte - 1;
    public const int MostSignificantBitIndex = BitsPerByte - 1;
}

internal static class UtfSegmentLimits
{
    // Scalar limits, byte counts and UTF-16 character counts are separate concepts.
    public const int MaximumScalar = 0x10ffff;
    public const int MaximumEncodedBytes = 4;
    public const int MaximumUtf16Characters = 2;
    public const int Utf16BytesPerCharacter = 2;
    public const int Utf32BytesPerScalar = 4;
}
