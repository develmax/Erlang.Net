namespace Erlang.Tests;

internal static class QueueCooperativeHostNames
{
    internal const string Test = "runtime/queue/core/cooperative-cancellation-cleanup";
    internal const string Failure = "Guest queue traversal did not yield, cancel or physically unwind as required.";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string Normal = "normal";
    internal const int Cells = 256017;
    internal const int MissingMember = 1;
    internal const int MinimumObservedTraversalReductions = 1024;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
