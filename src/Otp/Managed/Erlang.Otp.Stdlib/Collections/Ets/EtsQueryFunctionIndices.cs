namespace Erlang;

// Pinned ets.beam local-function identity indices, not execution counters.
internal static class EtsQueryFunctionIndices
{
    internal const int TraverseKeys = 0;
    internal const int SelectWithMatchSpec = 1;
    internal const int SelectWithDefaultMatchSpec = 2;
    internal const int BeforeTraversal = 3;
    internal const int AfterTraversal = 4;
    internal const int Information = 5;
    internal const int Lookup = 6;
    internal const int Format = 7;
    internal const int NextKey = 8;
    internal const int SelectContinuation = 9;
}
