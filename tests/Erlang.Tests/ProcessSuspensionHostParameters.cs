namespace Erlang.Tests;

internal static class ProcessSuspensionHostParameters
{
    public const int DeadlineMilliseconds = 200;
    public const int PausedWaitMilliseconds = 300;
    public static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);
}
