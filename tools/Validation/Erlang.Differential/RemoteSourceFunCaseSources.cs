namespace Erlang.Differential;

public static class RemoteSourceFunCaseSources
{
    public const string LexicalRoundtrip = "-module(source_fun_lexical_contract).\n-export([run/0, make/1, make_shadow/1, make_match/1]).\nmake(X) -> fun(Y) -> Z = Y + 1, {X, Z} end.\nmake_shadow(X) -> fun(X) -> X end.\nmake_match(X) -> fun() -> X = outer, X end.\nrun() ->\n    A = make(10), B = make(20),\n    A2 = binary_to_term(term_to_binary(A)),\n    B2 = binary_to_term(term_to_binary(B)),\n    {10, 4} = A2(3), {20, 5} = B2(4),\n    X = outer,\n    Shadow = fun(X) -> X end,\n    inner = Shadow(inner),\n    Match = fun() -> X = outer, X end,\n    outer = Match(), ok.\n";
}
