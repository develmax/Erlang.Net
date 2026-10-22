namespace Erlang.Tests;

internal static class EtsTraversal416HostNames
{
    internal const string OrderedTest = "runtime/ets-traversal416/ordered-eight-native-numeric-gaps-and-end-sentinel";
    internal const string HashTest = "runtime/ets-traversal416/real-hash-key-groups-lookup-and-source-aliases";
    internal const string RightsTest = "runtime/ets-traversal416/private-read-refusal-and-detached-returned-rows";
    internal const string RegistrationTest = "runtime/ets-traversal416/foreign-conflict-rollback-default-unchanged";
    internal const string Ets = "ets", New = "new", Insert = "insert", Delete = "delete", Lookup = "lookup";
    internal const string First = "first", Last = "last", Next = "next", Previous = "prev";
    internal const string FirstLookup = "first_lookup", LastLookup = "last_lookup", NextLookup = "next_lookup", PreviousLookup = "prev_lookup";
    internal const string Table = "traversal_host416", Ordered = "ordered_set", Set = "set", Bag = "bag", Duplicate = "duplicate_bag", Private = "private";
    internal const string End = "$end_of_table", Failure = "ETS real traversal416 source contract control failed";
    internal static readonly (string Function, int Arity)[] Signatures = [(First, 1), (Last, 1), (Next, 2), (Previous, 2), (FirstLookup, 1), (LastLookup, 1), (NextLookup, 2), (PreviousLookup, 2)];
}
