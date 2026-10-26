namespace Erlang;

internal static class EtsTraversalNames416
{
    internal const string Module = "ets", First = "first", Last = "last", Next = "next", Previous = "prev";
    internal const string FirstLookup = "first_lookup", LastLookup = "last_lookup", NextLookup = "next_lookup", PreviousLookup = "prev_lookup";
    internal const string End = "$end_of_table";
    internal const string Foreign = "ETS traversal requires the exact runtime and registry";
    internal const string Conflict = "ETS traversal refuses existing or source-owned bindings";
    internal const string Signature = "ETS traversal signature is not admitted";
    internal static readonly (string Function, int Arity)[] Signatures = [(First, 1), (Last, 1), (Next, 2), (Previous, 2), (FirstLookup, 1), (LastLookup, 1), (NextLookup, 2), (PreviousLookup, 2)];
}
