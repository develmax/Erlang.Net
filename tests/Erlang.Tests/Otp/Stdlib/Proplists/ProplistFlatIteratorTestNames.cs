namespace Erlang.Tests;

internal static class ProplistFlatIteratorTestNames
{
    internal const string Resource = "Erlang.Tests.ProplistFamily.proplists_flat_iterator_contract.erl";
    internal const string File = "proplists_flat_iterator_contract.erl";
    internal static readonly (string Name, string Input, string? Expected)[] Cases =
    [
        ("flat_nonzero", "[1|#{a=>1}]", "[{a,1}]"),
        ("flat_at_end", "[2|#{a=>1,b=>2}]", "[{a,1},{b,2}]"),
        ("flat_outside", "[2|#{a=>1}]", null),
        ("empty_zero", "[0|#{}]", "[]"),
        ("empty_outside", "[1|#{}]", null),
        ("flat_negative", "[-1|#{a=>1}]", null)
    ];
}
