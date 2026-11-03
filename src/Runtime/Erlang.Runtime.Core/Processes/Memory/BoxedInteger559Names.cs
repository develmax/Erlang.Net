namespace Erlang;

internal static class BoxedInteger559Names
{
    internal const int HeaderWords = 1;
    internal const int DigitBits = 64;
    internal const int DigitBytes = 8;
    internal const int ByteBits = 8;
    internal const int SmallMagnitudeBits = 59;
    internal const int MaximumDigits = (1 << 16) - 1;
    internal const int HeaderArityShift = 6;
    internal const nuint PositiveSubtag = 0x08;
    internal const nuint NegativeSubtag = 0x0c;

    internal static nuint Header(int digits, bool negative)
        => ((nuint)digits << HeaderArityShift) | (negative ? NegativeSubtag : PositiveSubtag);
}
