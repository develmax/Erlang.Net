namespace Erlang;

internal static class PacketHttpSyntax
{
    public const byte Space = (byte)' ';
    public const byte Tab = (byte)'\t';
    public const byte Newline = (byte)'\n';
    public const byte CarriageReturn = (byte)'\r';
    public const byte Colon = (byte)':';
    public const byte Dot = (byte)'.';
    public const byte Slash = (byte)'/';
    public const byte Hyphen = (byte)'-';
    public const byte OpenBracket = (byte)'[';
    public const byte CloseBracket = (byte)']';
    public const byte Star = (byte)'*';
    public const byte DigitZero = (byte)'0';
    public const byte DigitNine = (byte)'9';
    public const byte LowerA = (byte)'a';
    public const byte LowerZ = (byte)'z';
    public const byte UpperA = (byte)'A';
    public const byte UpperZ = (byte)'Z';
    public const int AsciiCaseDelta = 'a' - 'A';
    public const int AsciiUpperBound = 128;
    public const int DecimalBase = 10;
    public const int MaximumNormalizedName = 50;
    public const int MinimumVersionLength = 8;
    public const int LegacyMajor = 0;
    public const int LegacyMinor = 9;
    public static ReadOnlySpan<byte> VersionPrefix => "HTTP/"u8;
    public static ReadOnlySpan<byte> HttpPrefix => "http://"u8;
    public static ReadOnlySpan<byte> HttpsPrefix => "https://"u8;
    public static ReadOnlySpan<byte> DefaultPath => "/"u8;
    public static ReadOnlySpan<byte> SpecialCharacters => "()<>@,;:\\\"/[]?={} \t"u8;
}
