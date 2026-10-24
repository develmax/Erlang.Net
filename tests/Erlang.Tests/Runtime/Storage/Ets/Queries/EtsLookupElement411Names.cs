namespace Erlang.Tests;

internal static class EtsLookupElement411Names
{
    internal const long WideSecond = (1L << 32) + 2;
    internal const long NegativeNarrowed = (1L << 31) + 1;
    internal const long ZeroProjected = 1L << 32;
    internal const string HeaderDiagnostic = "The pinned narrowed ETS index addresses a tuple header; this runtime does not expose header words as guest terms";
    internal const string BoundaryTest = "runtime/ets-lookup-element411/host-zero-projection-representation-refusal";
    internal const string Function = "lookup_element";
    internal const string Table = "ets_lookup_element411";
    internal const string New = "new";
    internal const string Insert = "insert";
    internal const string Delete = "delete";
    internal const string Ordered = "ordered_set";
    internal const string Bag = "bag";
    internal const string DuplicateBag = "duplicate_bag";
    internal const string Private = "private";
    internal const string KeyPosition = "keypos";
    internal const string Missing = "missing_lookup_element411";
    internal const string ValuesTest = "runtime/ets-lookup-element411/sets-bags-keypos-numeric-default";
    internal const string ErrorsTest = "runtime/ets-lookup-element411/table-position-precedence-and-bag-short-row";
    internal const string ResourcesTest = "runtime/ets-lookup-element411/detached-copy-after-table-retirement";
}
