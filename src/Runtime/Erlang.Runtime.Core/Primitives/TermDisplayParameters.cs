namespace Erlang;

internal static class TermDisplayParameters
{
    public const int ByteMaximum = byte.MaxValue;
    public const int NativeFlatMapMaximumEntries = 32;
    public const int Space = 32;
    public const int Delete = 127;
    public const int ExtendedControlStart = 128;
    public const int ExtendedControlEnd = 159;
    public const int LatinLowerStart = 223;
    public const int LatinUpperStart = 192;
    public const int LatinUpperEnd = 222;
    public const int Division = 247;
    public const int Multiplication = 215;
    public const int BitsPerByte = 8;
    public const int OctalDigits = 3;
    public const int OctalRadix = 8;
    public const int ExponentMinimumDigits = 2;
    public const int ExponentPrefixLength = 2;
    public const byte LineFeed = 10;
}
