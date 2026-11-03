namespace Erlang.Tests;

internal static class ForeignRuntimeDrain606Names
{
    internal const string Case = "runtime/mailbox606/foreign-runtime-dispose-inside-actor-settles";
    internal const string MissingActorScheduler = "Foreign-runtime disposal control did not enter a runtime actor scheduler.";
    internal const string Incomplete = "Foreign runtime did not finish physical shutdown before the actor join returned.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(4);
    internal static readonly TimeSpan CompletionDeadline = TimeSpan.FromSeconds(7);
}
