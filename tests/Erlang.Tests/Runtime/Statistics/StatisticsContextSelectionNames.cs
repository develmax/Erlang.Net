namespace Erlang.Tests;

internal static class StatisticsContextSelectionNames
{
    internal const string Interleaved = "runtime/statistics-context-selection/interleaved-same-epoch";
    internal const string Adjacent = "runtime/statistics-context-selection/adjacent-same-epoch";
    internal const string Inline = "runtime/statistics-context-selection/initializer-and-inline-excluded";
    internal const string Spawn = "SpawnTraced";
    internal const string InitialPriority = "initialPriority";
    internal const string High = "High";
    internal const string Scheduler = "ActorTaskScheduler";
    internal const string Continue = "Continue";
    internal const string Continuation = "Continuation";
    internal const string TurnType = "Erlang.RuntimeActorExecutionTurn";
    internal const string Epoch = "Epoch";
    internal const string Failure = "Actual selected process dispatch counter or epoch witness differs";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
