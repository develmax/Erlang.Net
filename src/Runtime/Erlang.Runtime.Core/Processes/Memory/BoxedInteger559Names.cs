namespace Erlang;

internal static class BoxedInteger559Names
{
    internal const int HeaderWords = 1;
    internal static int DigitBits => NativeWordLayout.Bits;
    internal static int DigitBytes => NativeWordLayout.Bytes;
    internal const int ByteBits = 8;
    internal static int SmallMagnitudeBits => NativeWordLayout.Bits - 5;
    internal const int MaximumDigits = (1 << 16) - 1;
    internal const int HeaderArityShift = 6;
    internal const nuint PositiveSubtag = 0x08;
    internal const nuint NegativeSubtag = 0x0c;

    internal static nuint Header(int digits, bool negative)
        => ((nuint)digits << HeaderArityShift) | (negative ? NegativeSubtag : PositiveSubtag);
}
