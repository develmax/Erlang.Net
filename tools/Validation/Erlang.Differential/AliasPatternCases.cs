namespace Erlang.Differential;

public static class AliasPatternCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new(
            "alias-case-whole-and-parts",
            "case {1,2} of Whole={A,B} -> {Whole,A,B} end",
            Success(Term.Tuple(Term.Tuple(Term.I(1),Term.I(2)),Term.I(1),Term.I(2)))
        ),
        new(
            "alias-reversed-case",
            "case {1,2} of {A,B}=Whole -> {Whole,A,B} end",
            Success(Term.Tuple(Term.Tuple(Term.I(1),Term.I(2)),Term.I(1),Term.I(2)))
        ),
        new(
            "alias-nested-case",
            "case {1,{2,3}} of {A,Inner={B,C}}=Whole -> {A,Inner,B,C,Whole} end",
            Success(Term.Tuple(
                Term.I(1),
                Term.Tuple(Term.I(2),Term.I(3)),
                Term.I(2),
                Term.I(3),
                Term.Tuple(Term.I(1),Term.Tuple(Term.I(2),Term.I(3)))
            ))
        ),
        new("alias-structural-pair", "case {1,0} of {A,0}={1,B} -> {A,B} end", Success(Term.Tuple(Term.I(1),Term.I(0)))),
        new(
            "alias-mismatch-fallback",
            "case {1,2} of Whole={X,X} -> Whole; Other -> Other end",
            Success(Term.Tuple(Term.I(1),Term.I(2)))
        ),
        new("alias-exact-repeated-variable", "case {1,1.0} of {X,_}={_,X} -> no; _ -> yes end", Success(Term.A("yes"))),
        new(
            "alias-fun-argument",
            "(fun(T={X,X}) -> {T,X}; (_) -> no end)({7,7})",
            Success(Term.Tuple(Term.Tuple(Term.I(7),Term.I(7)),Term.I(7)))
        ),
        new(
            "alias-fun-shadow",
            "case 42 of X -> (fun(X={A,B}) -> {X,A,B} end)({1,2}) end",
            Success(Term.Tuple(Term.Tuple(Term.I(1),Term.I(2)),Term.I(1),Term.I(2)))
        ),
        new(
            "alias-maybe-match",
            "maybe Whole={ok,X} ?= {ok,42},{Whole,X} end",
            Success(Term.Tuple(Term.Tuple(Term.A("ok"),Term.I(42)),Term.I(42)))
        ),
        new(
            "alias-maybe-mismatch",
            "maybe Whole={X,X} ?= {1,2},Whole else Other -> Other end",
            Success(Term.Tuple(Term.I(1),Term.I(2)))
        ),
        new(
            "alias-maybe-else",
            "maybe ok ?= {error,7} else Whole={error,N} -> {Whole,N} end",
            Success(Term.Tuple(Term.Tuple(Term.A("error"),Term.I(7)),Term.I(7)))
        ),
        new(
            "alias-try-catch-stack",
            "try error({tag,7}) catch error:R={tag,N}:S -> {R,N,is_list(S)} end",
            Success(Term.Tuple(Term.Tuple(Term.A("tag"),Term.I(7)),Term.I(7),Term.A("true")))
        ),
        new("prefix-string-suffix", "case \"hello\" of \"he\" ++ Tail -> Tail end", Success(Term.String("llo"))),
        new(
            "prefix-string-alias-whole",
            "maybe Y=\"he\"++X=Z ?= \"hello\",{X,Y,Z} end",
            Success(Term.Tuple(Term.String("llo"),Term.String("hello"),Term.String("hello")))
        ),
        new(
            "prefix-string-alias-suffix",
            "maybe Y=\"he\"++(X=Z) ?= \"hello\",{X,Y,Z} end",
            Success(Term.Tuple(Term.String("llo"),Term.String("hello"),Term.String("llo")))
        ),
        new("prefix-integer-list-improper-tail", "case [1,2|tail] of [1,2] ++ Rest -> Rest end", Success(Term.A("tail"))),
        new("prefix-nested-proper-literal", "case [1,2,3] of [1|[2]] ++ Rest -> Rest end", Success(Term.List(Term.I(3)))),
        new("prefix-empty-list", "case {tag,7} of [] ++ Rest -> Rest end", Success(Term.Tuple(Term.A("tag"),Term.I(7)))),
        new("prefix-empty-string", "case tail of \"\" ++ Rest -> Rest end", Success(Term.A("tail"))),
        new("prefix-chained", "case [1,2,3] of [1] ++ [2] ++ Rest -> Rest end", Success(Term.List(Term.I(3)))),
        new("prefix-mismatch-fallback", "case \"hi\" of \"he\" ++ _ -> no; \"h\" ++ T -> T end", Success(Term.String("i"))),
        new("prefix-unicode", "case \"😀x\" of \"😀\" ++ Rest -> Rest end", Success(Term.String("x"))),
        new(
            "alias-map-incoming-key",
            "case a of K -> case #{a=>42} of Whole=#{K := V} -> {Whole,V} end end",
            Success(Term.Tuple(new MapTerm([new(Term.A("a"),Term.I(42))]),Term.I(42)))
        ),
        new(
            "alias-bit-incoming-size",
            "case 8 of S -> case <<42>> of Whole = <<X:S>> -> {Whole,X} end end",
            Success(Term.Tuple(new BitString([42]),Term.I(42)))
        )
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_alias_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).Append(new CompiledModuleCase(
        "compiled-alias-function-parameter",
        "-module(oracle_alias_parameter). -export([run/0]). run()->pick({7,7}). pick(T={X,X})->{T,X}; pick(_)->no.",
        Success(Term.Tuple(Term.Tuple(Term.I(7), Term.I(7)), Term.I(7)))
    )).ToArray();
}
