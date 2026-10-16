namespace Erlang.Otp;

internal static class ProcLibReportControlNames
{
    public const char Prefix = '~';
    public const char Precision = '.';
    public const char UnicodeModifier = 't';
    public const char Text = 's';
    public const char Pretty = 'p';
    public const char DepthPretty = 'P';
    public const char Write = 'w';
    public const string LimitedWriteOutsideReportSubset = "Finite write controls are outside the proc_lib report control subset.";
    public const char Newline = '\n';
    public const char Tab = '\t';
    public const int DefaultWidth = 80;
    public const int TabWidth = 8;
    public const int Unlimited = -1;
    public const int EllipsisLength = 3;
    public const int EllipsisLookahead = 4;
    public const string Ellipsis = "...";
    public const string Unicode = "unicode";
    public const int DigitRadix = 10;
    public const char FirstDigit = '0';
    public const char LastDigit = '9';
    public const int Latin1Maximum = 255;
    public const string LimitedUnicodeTextOutsideReportSubset = "Finite Unicode text controls are outside the proc_lib report control subset.";
}
