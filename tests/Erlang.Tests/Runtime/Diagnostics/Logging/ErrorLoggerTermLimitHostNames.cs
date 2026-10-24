namespace Erlang.Tests;

internal static class ErrorLoggerTermLimitHostNames
{
    public const string Test = "error_logger term limit preserves source fast path and iterator subset";
    public const string Type = "Erlang.Otp.ErrorLoggerTermLimiter";
    public const string Method = "Limit";
    public const string First = "first";
    public const string Second = "second";
    public const string Missing = "The term limiter did not preserve the source identity or iterator contract.";
    public const int FormatDepth = 10;
    public const int SmallDepth = 2;
    public const int LargeMapSize = 64;
    public const int NestedLevels = 2048;
    public const int SufficientDepth = 4096;
    public const int DeepKeyLevels = 24;
}
