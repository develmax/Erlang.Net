namespace Erlang.Otp;

internal static class ErrorLoggerTermLimitNames
{
    public const string Module = "error_logger";
    public const string LimitTerm = "limit_term";
    public const string GetFormatDepth = "get_format_depth";
    public const string Unlimited = "unlimited";
    public const string Ellipsis = "...";
    public const int MinimumFormatDepth = 10;
    public const int UnlimitedDepth = -1;
    public const int PrintableAsciiFirst = 32;
    public const int PrintableAsciiLast = 126;
    public const int PrintableLatin1First = 160;
    public const int PrintableLatin1Last = 255;
}
