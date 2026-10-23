namespace Erlang.Otp;

internal static class ProcLibReportCharacterParameters
{
    public const int Latin1Maximum = 255;
    public const int PrintableAsciiFirst = 32;
    public const int PrintableAsciiLast = 126;
    public const int PrintableLatin1First = 160;
    public const int UnicodeMaximum = 0x10ffff;
    public const int SurrogateFirst = 0xd800;
    public const int SurrogateLast = 0xdfff;
    public const int SurrogateGraphemeProxyFirst = 0xf0000;
    public const int LowerLatin1First = 223;
    public const int LowerLatin1Last = 255;
    public const int UpperLatin1First = 192;
    public const int UpperLatin1Last = 222;
    public const int DivisionSign = 247;
    public const int MultiplicationSign = 215;
}
