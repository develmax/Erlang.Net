namespace Erlang.Tests;

internal static class CommonErrorCaptureHostParameters
{
    internal const int DefaultDepth = 8;
    internal const int MinimumDepth = 1;
    internal const int Payload = 42;
    internal static TimeSpan Deadline => TimeSpan.FromSeconds(10);
}
