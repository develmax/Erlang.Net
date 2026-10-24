namespace Erlang.Tests;

internal static class BacktraceDepthHostParameters
{
    internal const int DefaultDepth = 8;
    internal const int FirstDepth = 0;
    internal const int SecondDepth = 1;
    internal static TimeSpan Deadline => TimeSpan.FromSeconds(5);
}
