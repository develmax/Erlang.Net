namespace Erlang.Differential;

public static class ExpressionListCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Error(Term reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), reason);

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("tuple-export", "begin {X=1,Y=2},{X,Y} end", Success(Term.Tuple(Term.I(1),Term.I(2)))),
        new("tuple-equal-binding", "begin {X=1,X=1},X end", Success(Term.I(1))),
        new("tuple-conflict", "{X=1,X=2}", Error(Term.Tuple(Term.A("badmatch"),Term.I(1)))),
        new("tuple-exact-conflict", "{X=1,X=1.0}", Error(Term.Tuple(Term.A("badmatch"),Term.I(1)))),
        new(
            "tuple-conflict-stops-next",
            "begin put(mark,none),R=catch {X=1,X=2,put(mark,late)},{'EXIT',{Reason,_}}=R,{Reason,get(mark)} end",
            Success(Term.Tuple(Term.Tuple(Term.A("badmatch"),Term.I(1)),Term.A("none")))
        ),
        new(
            "tuple-side-effect-order",
            "begin put(mark,0),T={put(mark,1),put(mark,2)},{T,get(mark)} end",
            Success(Term.Tuple(Term.Tuple(Term.I(0),Term.I(1)),Term.I(2)))
        ),
        new("tuple-nested-export", "begin {X=1,{Y=2,Z=3}},{X,Y,Z} end", Success(Term.Tuple(Term.I(1),Term.I(2),Term.I(3)))),
        new("tuple-prebound", "begin X=1,{X,X=1} end", Success(Term.Tuple(Term.I(1),Term.I(1)))),
        new("call-export", "begin T=element(X=1,Y={2}),{T,X,Y} end", Success(Term.Tuple(Term.I(2),Term.I(1),Term.Tuple(Term.I(2))))),
        new(
            "call-equal-binding",
            "begin T=lists:append(X=[1],X=[1]),{T,X} end",
            Success(Term.Tuple(Term.List(Term.I(1),Term.I(1)),Term.List(Term.I(1))))
        ),
        new("call-conflict", "erlang:'+'(X=1,X=2)", Error(Term.Tuple(Term.A("badmatch"),Term.I(1)))),
        new("call-first-error", "erlang:'+'(error(left),error(right))", Error(Term.A("left"))),
        new("call-nested-export", "begin T=element(1,{X=42}),{T,X} end", Success(Term.Tuple(Term.I(42),Term.I(42)))),
        new(
            "apply-argument-export",
            "begin F=fun(A,B)->A+B end,T=F(X=1,Y=2),{T,X,Y} end",
            Success(Term.Tuple(Term.I(3),Term.I(1),Term.I(2)))
        ),
        new("sequential-body-control", "begin X=1,Y=X+1,{X,Y} end", Success(Term.Tuple(Term.I(1),Term.I(2)))),
        new("tuple-closure-export", "begin {X=42,F=fun()->ok end},{X,F()} end", Success(Term.Tuple(Term.I(42),Term.A("ok")))),
        new("list-export", "begin [X=1,Y=2],{X,Y} end", Success(Term.Tuple(Term.I(1),Term.I(2)))),
        new("list-tail-export", "begin [X=1|Y=tail],{X,Y} end", Success(Term.Tuple(Term.I(1),Term.A("tail")))),
        new("list-conflict-right-value", "[X=1,X=2]", Error(Term.Tuple(Term.A("badmatch"),Term.I(2)))),
        new(
            "list-conflict-after-tail-effects",
            "begin put(mark,none),R=catch [X=1,X=2,put(mark,late)],{'EXIT',{Reason,_}}=R,{Reason,get(mark)} end",
            Success(Term.Tuple(Term.Tuple(Term.A("badmatch"),Term.I(2)),Term.A("late")))
        )
    ];
}
