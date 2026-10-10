namespace Erlang.Differential;

public static class ListComprehensionCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Failure(string reason, Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), Term.Tuple(Term.A(reason), value));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("lc-basic", "[X*2 || X <- [1,2,3]]", Success(Term.List(Term.I(2),Term.I(4),Term.I(6)))),
        new("lc-ordered-filter", "[X || X <- [1,2,3,4],X rem 2=:=0]", Success(Term.List(Term.I(2),Term.I(4)))),
        new("lc-empty", "[X || X <- []]", Success(Term.List())),
        new("lc-no-generator", "[42 || true]", Success(Term.List(Term.I(42)))),
        new("lc-false-filter", "[42 || false]", Success(Term.List())),
        new("lc-guard-nonboolean", "[42 || 1]", Success(Term.List())),
        new("lc-guard-failure-skips", "[X || X <- [[],[1],atom],hd(X)=:=1]", Success(Term.List(Term.List(Term.I(1))))),
        new("lc-ordinary-nonboolean", "[42 || begin 1 end]", Failure("bad_filter",Term.I(1))),
        new(
            "lc-ordinary-error",
            "[X || X <- [1],begin error(filter_failed) end]",
            Term.Tuple(Term.A("error"),Term.A("error"),Term.A("filter_failed"))
        ),
        new("lc-ordinary-fun-filter", "[X || X <- [1,2,3],(fun(N)->N>1 end)(X)]", Success(Term.List(Term.I(2),Term.I(3)))),
        new("lc-relaxed-mismatch", "[X || {ok,X} <- [bad,{ok,1},{error,2},{ok,3}]]", Success(Term.List(Term.I(1),Term.I(3)))),
        new("lc-repeated-exact", "[X || {X,X} <- [{1,1},{1,1.0},{2,2}]]", Success(Term.List(Term.I(1),Term.I(2)))),
        new("lc-strict-success", "[X || {ok,X} <:- [{ok,1},{ok,2}]]", Success(Term.List(Term.I(1),Term.I(2)))),
        new("lc-strict-mismatch", "[X || {ok,X} <:- [{ok,1},bad,{ok,2}]]", Failure("badmatch",Term.A("bad"))),
        new("lc-strict-filter-not-match", "[X || X <:- [1,2,3],X>1]", Success(Term.List(Term.I(2),Term.I(3)))),
        new("lc-bad-source", "[X || X <- atom]", Failure("bad_generator",Term.A("atom"))),
        new("lc-improper-source", "[X || X <- [1,2|tail]]", Failure("bad_generator",Term.A("tail"))),
        new("lc-empty-before-bad-source", "[{X,Y} || X <- [],Y <- atom]", Success(Term.List())),
        new(
            "lc-cartesian-order",
            "[{X,Y} || X <- [1,2],Y <- [a,b]]",
            Success(Term.List(
                Term.Tuple(Term.I(1),Term.A("a")),
                Term.Tuple(Term.I(1),Term.A("b")),
                Term.Tuple(Term.I(2),Term.A("a")),
                Term.Tuple(Term.I(2),Term.A("b"))
            ))
        ),
        new(
            "lc-dependent-generator",
            "[{X,Y} || X <- [1,2],Y <- [X,X+10]]",
            Success(Term.List(
                Term.Tuple(Term.I(1),Term.I(1)),
                Term.Tuple(Term.I(1),Term.I(11)),
                Term.Tuple(Term.I(2),Term.I(2)),
                Term.Tuple(Term.I(2),Term.I(12))
            ))
        ),
        new(
            "lc-shadow-outer",
            "begin X=9,R=[X || X <- [1,2]],{X,R} end",
            Success(Term.Tuple(Term.I(9),Term.List(Term.I(1),Term.I(2))))
        ),
        new("lc-shadow-earlier-generator", "[X || X <- [1,2],X <- [X+10]]", Success(Term.List(Term.I(11),Term.I(12)))),
        new("lc-filter-binding", "[Y || X <- [1,2],begin Y=X+1,true end]", Success(Term.List(Term.I(2),Term.I(3)))),
        new("lc-source-binding-private", "[X || X <- begin Y=[1,2],Y end]", Success(Term.List(Term.I(1),Term.I(2)))),
        new(
            "lc-nested",
            "[[Y || Y <- [X,X+1]] || X <- [1,2]]",
            Success(Term.List(Term.List(Term.I(1),Term.I(2)),Term.List(Term.I(2),Term.I(3))))
        ),
        new("lc-multiple-templates", "[X,X+10 || X <- [1,2]]", Success(Term.List(
            Term.I(1),
            Term.I(11),
            Term.I(2),
            Term.I(12)
        ))),
        new("lc-template-match-private", "[Y=X+1 || X <- [1,2]]", Success(Term.List(Term.I(2),Term.I(3)))),
        new(
            "lc-body-side-effect-order",
            "begin put(mark,0),R=[begin put(mark,get(mark)+1),X end || X <- [1,2,3]],{R,get(mark)} end",
            Success(Term.Tuple(Term.List(Term.I(1),Term.I(2),Term.I(3)),Term.I(3)))
        ),
        new(
            "lc-alias-pattern",
            "[{Whole,X} || Whole={ok,X} <- [bad,{ok,7}]]",
            Success(Term.List(Term.Tuple(Term.Tuple(Term.A("ok"),Term.I(7)),Term.I(7))))
        ),
        new("lc-map-input-key", "begin K=a,[V || #{K:=V} <- [#{a=>1},#{b=>2},#{a=>3}]] end", Success(Term.List(Term.I(1),Term.I(3)))),
        new("lc-bit-input-size", "begin S=8,[X || <<X:S>> <- [<<1>>,<<2>>,<<3:4>>]] end", Success(Term.List(Term.I(1),Term.I(2)))),
        new(
            "lc-closure-capture",
            "begin F=[fun()->X end || X <- [1,2]],[(hd(F))(),(hd(tl(F)))()] end",
            Success(Term.List(Term.I(1),Term.I(2)))
        ),
        new(
            "lc-failure-before-improper-tail",
            "[X || X <- [1|tail],begin error(first) end]",
            Term.Tuple(Term.A("error"),Term.A("error"),Term.A("first"))
        ),
        new("lc-false-before-improper-tail", "[X || X <- [1|tail],false]", Failure("bad_generator",Term.A("tail"))),
        new("lc-remote-operator-filter", "[X || X <- [1,2,3],erlang:'>'(X,1)]", Success(Term.List(Term.I(2),Term.I(3)))),
        new("lc-remote-arithmetic-guard-failure", "[X || X <- [1,atom],erlang:'+'(X,1)>1]", Success(Term.List(Term.I(1))))
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_lc_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).ToArray();
}
