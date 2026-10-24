namespace Erlang.Tests;

internal static class ActorControlOwner428Names
{
    internal const string Test = "runtime/actor-control428-real-running-exclusion-and-ready-queue";
    internal const string DispatcherField = "actorDispatcher";
    internal const string AcquireMethod = "TryAcquireControl428";
    internal const string SchedulerProperty = "ActorTaskScheduler";
    internal const string PendingProperty = "Pending";
    internal const string Failure = "Actor control ownership did not exclude actual normal dispatch";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
