namespace Erlang;

internal static class ReferenceTextSyntax
{
    public const string Opening = "#Ref<";
    public const char Closing = '>';
    public const char Separator = '.';
    public const byte NullTerminator = 0;
    public const byte LowestDigit = (byte)'0';
    public const byte HighestDigit = (byte)'9';
    public static ReadOnlySpan<byte> Prefix => "#Ref<"u8;
}
