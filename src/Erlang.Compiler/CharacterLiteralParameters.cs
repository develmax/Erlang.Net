namespace Erlang.Compiler;

internal static class CharacterLiteralParameters
{
    public const char Marker = '$';
    public const char Escape = '\\';
    public const char HexMarker = 'x';
    public const char HexOpening = '{';
    public const char HexClosing = '}';
    public const char ControlMarker = '^';
    public const char FirstOctalDigit = '0';
    public const char LastOctalDigit = '7';
    public const int OctalRadix = 8;
    public const int MaximumOctalDigits = 3;
    public const int HexRadix = 16;
    public const int FixedHexDigits = 2;
    public const int ControlMask = 0x1f;
    public const int DeleteCodepoint = 0x7f;
    public const int EscapeCodepoint = 0x1b;
    public const int HexAlphabetOffset = 10;
    public const int MaximumUnicodeScalar = 0x10ffff;
    public const int FirstSurrogate = 0xd800;
    public const int LastSurrogate = 0xdfff;
    public const int IllegalBmpCodepoint = 0xfffe;
    public const int IllegalBmpCodepointNext = 0xffff;
}
