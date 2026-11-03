namespace Erlang.Tests;

internal static class RegexForeignEngine605Names
{
    internal const string Case = "runtime/regex605/direct-foreign-actor-engine-disposal-settles";
    internal const string MissingScheduler = "Direct engine disposal control did not enter a real actor scheduler.";
    internal const string Incomplete = "Direct foreign engine disposal did not finish physical cleanup.";
    internal static readonly TimeSpan JoinDeadline = TimeSpan.FromSeconds(4);
    internal static readonly TimeSpan CompletionDeadline = TimeSpan.FromSeconds(7);
}
