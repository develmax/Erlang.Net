namespace Erlang;

internal static class ProcessIdentifierTextSyntax
{
    public const char PidOpening = '<';
    public const char Closing = '>';
    public const char Separator = '.';
    public const byte NullTerminator = 0;
    public const byte LowestDigit = (byte)'0';
    public const byte HighestDigit = (byte)'9';
    public const string PortOpening = "#Port<";
    public static ReadOnlySpan<byte> PidPrefix => "<"u8;
    public static ReadOnlySpan<byte> PortPrefix => "#Port<"u8;
}
