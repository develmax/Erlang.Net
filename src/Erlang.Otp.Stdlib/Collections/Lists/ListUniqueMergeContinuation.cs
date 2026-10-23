namespace Erlang;

internal sealed record ListUniqueMergeContinuation(
    ListUniqueMergeState State,
    Term[] Arguments,
    int ResultArgument,
    Term ResultTail,
    ListUniqueMergeState Caller,
    int Line
);
