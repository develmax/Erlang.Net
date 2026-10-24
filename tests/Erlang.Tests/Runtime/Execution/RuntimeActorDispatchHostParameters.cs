namespace Erlang.Tests;

internal static class RuntimeActorDispatchHostParameters
{
    internal static readonly int[] Capacities = [1, 2, 4];
    internal const int PerProcessConcurrency = 1;
    internal const int ExtraQueuedActors = 2;
    internal const int RollbackProcessLimit = 3;
    internal const int RollbackAttempts = 3;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
