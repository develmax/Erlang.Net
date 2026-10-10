namespace Erlang.Differential;

public static class MapComprehensionCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Failure(string reason, Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), Term.Tuple(Term.A(reason), value));

    private static Term Map(params (Term Key, Term Value)[] fields) => new MapTerm(fields.Select(field => new KeyValuePair<Term, Term>(field.Key, field.Value)));

    private static Term Numbers(params long[] values) => Cons.From(values.Select(Term.I));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("mc-list", "#{X=>X*2 || X <- [1,2,3]}", Success(Map((Term.I(1),Term.I(2)),(Term.I(2),Term.I(4)),(Term.I(3),Term.I(6))))),
        new("mc-filter", "#{X=>X || X <- [1,2,3],X>1}", Success(Map((Term.I(2),Term.I(2)),(Term.I(3),Term.I(3))))),
        new("mc-empty", "#{X=>X || X <- []}", Success(Map())),
        new("mc-no-generator", "#{a=>7 || true}", Success(Map((Term.A("a"),Term.I(7))))),
        new("mc-false", "#{a=>error(unreachable) || false}", Success(Map())),
        new(
            "mc-multiple-fields",
            "#{X=>X*2,X+10=>X*3 || X <- [1,2]}",
            Success(Map(
                (Term.I(1),Term.I(2)),
                (Term.I(2),Term.I(4)),
                (Term.I(11),Term.I(3)),
                (Term.I(12),Term.I(6))
            ))
        ),
        new("mc-last-key", "#{a=>X || X <- [1,2,3]}", Success(Map((Term.A("a"),Term.I(3))))),
        new("mc-last-field", "#{a=>X,a=>X+10 || X <- [1,2]}", Success(Map((Term.A("a"),Term.I(12))))),
        new(
            "mc-exact-numeric-keys",
            "#{X=>X || X <- [1,1.0]}",
            Success(Map((Term.I(1),Term.I(1)),(new FloatTerm(1),new FloatTerm(1))))
        ),
        new(
            "mc-tuple-key",
            "#{{X,X}=>X || X <- [1,2]}",
            Success(Map((Term.Tuple(Term.I(1),Term.I(1)),Term.I(1)),(Term.Tuple(Term.I(2),Term.I(2)),Term.I(2))))
        ),
        new(
            "mc-nested",
            "#{X=>#{Y=>Y || Y <- [X,X+1]} || X <- [1]}",
            Success(Map((Term.I(1),Map((Term.I(1),Term.I(1)),(Term.I(2),Term.I(2))))))
        ),
        new("mc-binary-source", "#{X=>X+1 || <<X>> <= <<1,2>>}", Success(Map((Term.I(1),Term.I(2)),(Term.I(2),Term.I(3))))),
        new(
            "mc-dependent",
            "#{Y=>X || X <- [1,2],Y <- [X,X+10]}",
            Success(Map(
                (Term.I(1),Term.I(1)),
                (Term.I(11),Term.I(1)),
                (Term.I(2),Term.I(2)),
                (Term.I(12),Term.I(2))
            ))
        ),
        new("mc-key-value-conflict", "#{(X=1)=>(X=2) || true}", Failure("badmatch",Term.I(1))),
        new(
            "mc-template-private",
            "begin X=9,R=#{X=>X || X <- [1,2]},{X,R} end",
            Success(Term.Tuple(Term.I(9),Map((Term.I(1),Term.I(1)),(Term.I(2),Term.I(2)))))
        ),
        new(
            "mc-effect-order",
            "begin put(mark,none),M=#{(begin put(mark,key),a end)=>(begin put(mark,value),get(mark) end) || true},{M,get(mark)} end",
            Success(Term.Tuple(Map((Term.A("a"),Term.A("value"))),Term.A("value")))
        ),
        new("mg-map-result", "#{K=>V*2 || K := V <- #{a=>1,b=>2}}", Success(Map((Term.A("a"),Term.I(2)),(Term.A("b"),Term.I(4))))),
        new(
            "mg-list-result",
            "[{K,V} || K := V <- #{1=>2,3=>4}]",
            Success(Term.List(Term.Tuple(Term.I(1),Term.I(2)),Term.Tuple(Term.I(3),Term.I(4))))
        ),
        new("mg-binary-result", "<< <<V>> || K := V <- #{1=>7,2=>8} >>", Success(new BitString(new byte[]{7,8}))),
        new("mg-empty", "[K || K := V <- #{}]", Success(Numbers())),
        new("mg-strict-empty", "[K || K := V <:- #{}]", Success(Numbers())),
        new("mg-key-filter", "[V || a := V <- #{a=>1,b=>2}]", Success(Numbers(1))),
        new("mg-value-filter", "[K || K := {ok,V} <- #{a=>{ok,1},b=>bad}]", Success(Term.List(Term.A("a")))),
        new("mg-strict-key", "[V || a := V <:- #{b=>2}]", Failure("badmatch",Term.Tuple(Term.A("b"),Term.I(2)))),
        new("mg-strict-value", "[K || K := {ok,V} <:- #{a=>bad}]", Failure("badmatch",Term.Tuple(Term.A("a"),Term.A("bad")))),
        new("mg-repeated", "[K || K := K <- #{1=>1,2=>3,4=>4}]", Success(Numbers(1,4))),
        new("mg-repeated-strict", "[K || K := K <:- #{2=>3}]", Failure("badmatch",Term.Tuple(Term.I(2),Term.I(3)))),
        new("mg-bad-map", "[K || K := V <- atom]", Failure("bad_generator",Term.A("atom"))),
        new("mg-bad-list", "[K || K := V <- [1]]", Failure("bad_generator",Numbers(1))),
        new(
            "mg-shadow",
            "begin K=9,V=8,R=[{K,V} || K := V <- #{1=>2}],{K,V,R} end",
            Success(Term.Tuple(Term.I(9),Term.I(8),Term.List(Term.Tuple(Term.I(1),Term.I(2)))))
        ),
        new(
            "mg-nested-source",
            "[{K,V} || M <- [#{1=>2},#{3=>4}],K := V <- M]",
            Success(Term.List(Term.Tuple(Term.I(1),Term.I(2)),Term.Tuple(Term.I(3),Term.I(4))))
        ),
        new(
            "mg-follow-list",
            "[{K,X} || K := V <- #{1=>[2,3]},X <- V]",
            Success(Term.List(Term.Tuple(Term.I(1),Term.I(2)),Term.Tuple(Term.I(1),Term.I(3))))
        ),
        new("mg-follow-binary", "[X || K := V <- #{1=><<2,3>>},<<X>> <= V]", Success(Numbers(2,3))),
        new("mg-key-incoming-scope", "begin A=a,[X || K := #{A:=X} <- #{1=>#{a=>7}}] end", Success(Numbers(7))),
        new("mg-guard-filter", "[K || K := V <- #{1=>1,2=>atom,3=>2},V+1>1]", Success(Numbers(1,3))),
        new("mg-ordinary-filter", "[K || K := V <- #{1=>1,2=>2},(fun(X)->X>1 end)(V)]", Success(Numbers(2))),
        new("mg-bad-filter", "[K || K := V <- #{1=>2},(fun()->wrong end)()]", Failure("bad_filter",Term.A("wrong"))),
        new("mg-filter-binding", "[Y || K := V <- #{1=>2},begin Y=V+1,true end]", Success(Numbers(3))),
        new("mg-none-iterator", "[K || K := V <- none]", Success(Numbers())),
        new(
            "mg-tuple-iterator",
            "[{K,V} || K := V <- {b,2,{a,1,none}}]",
            Success(Term.List(Term.Tuple(Term.A("b"),Term.I(2)),Term.Tuple(Term.A("a"),Term.I(1))))
        ),
        new(
            "mg-invalid-iterator-tail",
            "[K || K := V <- {a,1,bad}]",
            Failure("bad_generator",Term.Tuple(Term.A("a"),Term.I(1),Term.A("bad")))
        ),
        new("mg-ordered-iterator", "[K || K := V <- maps:iterator(#{3=>a,1=>b,2=>c},ordered)]", Success(Numbers(1,2,3))),
        new("mg-reversed-iterator", "[K || K := V <- maps:iterator(#{3=>a,1=>b,2=>c},reversed)]", Success(Numbers(3,2,1))),
        new("mg-default-iterator", "[K || K := V <- maps:iterator(#{1=>a,2=>b})]", Success(Numbers(1,2))),
        new(
            "mg-partial-iterator",
            "begin {_,_,Rest}=maps:next(maps:iterator(#{1=>a,2=>b},ordered)),[K || K := V <- Rest] end",
            Success(Numbers(2))
        ),
        new("mg-literal-key-iterator", "[K || K := V <- [[b,a]|#{a=>1,b=>2}]]", Success(Term.List(Term.A("b"),Term.A("a")))),
        new(
            "mc-source-binding-isolated",
            "begin M=#{1=>2},R=#{K=>V || K := V <- begin M= #{1=>2},M end},M end",
            Success(Map((Term.I(1),Term.I(2))))
        ),
        new(
            "mc-closure-capture",
            "begin F=[fun()->{K,V} end || K := V <- #{1=>2}],(hd(F))() end",
            Success(Term.Tuple(Term.I(1),Term.I(2)))
        ),
        new("mi-bad-map", "maps:iterator(atom)", Failure("badmap",Term.A("atom"))),
        new("mi-invalid-order", "maps:iterator(#{},wrong)", Term.Tuple(Term.A("error"),Term.A("error"),Term.A("badarg"))),
        new("mi-order-invalid-map", "maps:iterator(atom,ordered)", Term.Tuple(Term.A("error"),Term.A("error"),Term.A("badarg"))),
        new("mi-next-none", "maps:next(none)", Success(Term.A("none"))),
        new("mi-next-tuple", "maps:next({a,1,bad})", Success(Term.Tuple(Term.A("a"),Term.I(1),Term.A("bad")))),
        new("mi-next-invalid", "maps:next(wrong)", Term.Tuple(Term.A("error"),Term.A("error"),Term.A("badarg"))),
        new(
            "mg-validation-before-body",
            "begin put(mark,none),R=catch [put(mark,K) || K := V <- {a,1,bad}],{'EXIT',{Reason,_}}=R,{Reason,get(mark)} end",
            Success(Term.Tuple(Term.Tuple(Term.A("bad_generator"),Term.Tuple(Term.A("a"),Term.I(1),Term.A("bad"))),Term.A("none")))
        ),
        new(
            "mc-large-map",
            "#{K=>V+1 || K := V <- #{X=>X || X <- [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40]}}",
            Success(Map(Enumerable.Range(1,40).Select(key => ((Term)Term.I(key),(Term)Term.I(key+1))).ToArray()))
        )
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_mc_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Name == "mc-effect-order" ? Success(Term.Tuple(Map((Term.A("a"), Term.A("value"))), Term.A("key"))) : fixture.Expected
    )).ToArray();
}
