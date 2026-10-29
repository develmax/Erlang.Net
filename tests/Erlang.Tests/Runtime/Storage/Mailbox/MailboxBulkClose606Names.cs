using System.Reflection;

namespace Erlang.Tests;

internal static class MailboxBulkClose606Names
{
    internal const string Case = "runtime/mailbox606/bulk-commit-observer-close-exact-removal-and-cleanup";
    internal const string QueueObserver = "QueueLengthChanged";
    internal const string Stop = "StopForHalt";
    internal const string Failure = "Bulk mailbox commit reused a closed tail or reported retired entries as explicit removals.";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal static readonly TimeSpan Deadline = MailboxReentrantExit606Names.Deadline;
    internal const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
}
