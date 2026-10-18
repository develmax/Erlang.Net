namespace Erlang.Tests;

internal static class QueueMemberHostNames
{
    internal const string Test = "runtime/queue/member/own-entry-and-short-circuit";
    internal const string Failure = "Queue member did not preserve its actual BIF entry, strict equality or lazy branch.";
    internal const string Remaining = "RemainingListTraversalReductions";
    internal const string Missing = "queue_member_missing";
    internal const string Tail = "queue_member_improper_tail";
    internal const string Lists = "lists";
    internal const int StartingReductions = 1;
    internal const int BeforeBoundary = 15;
    internal const int AtBoundary = 16;
    internal const int AfterBoundary = 17;
    internal const int AfterSecondBoundary = 32017;
    internal const int FirstCommitted = 16;
    internal const int SecondCommitted = 32016;
}
