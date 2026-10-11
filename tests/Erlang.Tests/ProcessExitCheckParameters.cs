namespace Erlang.Tests;

internal static class ProcessExitCheckParameters
{
    public const int ConcurrentChildren = 128;
    public static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);
}
