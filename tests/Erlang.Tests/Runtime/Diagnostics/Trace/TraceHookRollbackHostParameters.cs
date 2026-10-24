namespace Erlang.Tests;

internal static class TraceHookRollbackHostParameters
{
    internal const int Capacity = 1;
    internal const int ProcessLimit = 4;
    internal const int FailureAttempts = 3;
    internal const int Label = 17;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
