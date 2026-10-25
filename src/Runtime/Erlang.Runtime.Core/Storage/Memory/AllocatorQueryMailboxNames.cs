namespace Erlang;

internal static class AllocatorQueryMailboxNames
{
    internal const string AlreadyStarted = "The allocator producer has already started.";
    internal const string Closed = "The allocator producer is no longer accepting replies.";
    internal const string ExcessReplies = "The allocator producer exceeded its prepared reply count.";
    internal const string MissingReplies = "The allocator producer did not post its prepared reply count.";
    internal const string UnexpectedReply = "A committed allocator reply did not match its query.";
    internal const string CleanupFailure = "AllocatorQueryCleanupFailure";
}
