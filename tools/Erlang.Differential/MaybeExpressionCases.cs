namespace Erlang.Differential;

public static class MaybeExpressionCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Failure(string exceptionClass, Term reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A(exceptionClass), reason);

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("maybe-ordinary-sequence", "maybe X=40,X+2 end", Success(Term.I(42))),
        new("maybe-match-last-value", "maybe {ok,X} ?= {ok,42} end", Success(Term.Tuple(Term.A("ok"),Term.I(42)))),
        new("maybe-sequential-matches", "maybe {ok,X} ?= {ok,40},{ok,Y} ?= {ok,X+2},Y end", Success(Term.I(42))),
        new(
            "maybe-first-mismatch",
            "maybe {ok,X} ?= {error,first},error(unreachable),X end",
            Success(Term.Tuple(Term.A("error"),Term.A("first")))
        ),
        new(
            "maybe-later-mismatch",
            "maybe {ok,X} ?= {ok,1},{ok,Y} ?= {error,X},Y end",
            Success(Term.Tuple(Term.A("error"),Term.I(1)))
        ),
        new("maybe-success-skips-else", "maybe {ok,X} ?= {ok,42},X else _ -> error(unreachable) end", Success(Term.I(42))),
        new("maybe-else-pattern", "maybe {ok,X} ?= {error,7},X else {error,N} -> N+1 end", Success(Term.I(8))),
        new("maybe-else-guard-alternatives", "maybe ok ?= 2 else N when N==1; N==2 -> yes; _ -> no end", Success(Term.A("yes"))),
        new(
            "maybe-else-guard-failure",
            "maybe ok ?= atom else N when hd(N)==1 -> no; N when is_atom(N) -> yes end",
            Success(Term.A("yes"))
        ),
        new(
            "maybe-else-no-match",
            "maybe ok ?= {error,7} else no -> unreachable end",
            Failure("error",Term.Tuple(Term.A("else_clause"),Term.Tuple(Term.A("error"),Term.I(7))))
        ),
        new(
            "maybe-else-all-guards-fail",
            "maybe ok ?= 2 else N when N==1 -> unreachable end",
            Failure("error",Term.Tuple(Term.A("else_clause"),Term.I(2)))
        ),
        new("maybe-rhs-error-propagates", "maybe ok ?= error(rhs) else _ -> wrong end", Failure("error",Term.A("rhs"))),
        new("maybe-rhs-throw-propagates", "maybe ok ?= throw(rhs) else _ -> wrong end", Failure("throw",Term.A("rhs"))),
        new("maybe-rhs-exit-propagates", "maybe ok ?= exit(rhs) else _ -> wrong end", Failure("exit",Term.A("rhs"))),
        new("maybe-body-error-propagates", "maybe error(body) else _ -> wrong end", Failure("error",Term.A("body"))),
        new(
            "maybe-ordinary-badmatch",
            "maybe ok=wrong else _ -> wrong end",
            Failure("error",Term.Tuple(Term.A("badmatch"),Term.A("wrong")))
        ),
        new("maybe-else-body-error", "maybe ok ?= wrong else _ -> error(else_body) end", Failure("error",Term.A("else_body"))),
        new("maybe-incoming-binding", "case 42 of X -> {maybe X ?= 42,X end,X} end", Success(Term.Tuple(Term.I(42),Term.I(42)))),
        new(
            "maybe-incoming-mismatch-else",
            "case 42 of X -> maybe X ?= 7 else N -> {X,N} end end",
            Success(Term.Tuple(Term.I(42),Term.I(7)))
        ),
        new("maybe-repeated-pattern-variable", "maybe {X,X} ?= {1,2} else V -> V end", Success(Term.Tuple(Term.I(1),Term.I(2)))),
        new("maybe-exact-numeric-match", "maybe 1 ?= 1.0 else V -> is_float(V) end", Success(Term.A("true"))),
        new(
            "maybe-list-tail-pattern",
            "maybe [H|T] ?= [1,2|tail],{H,T} end",
            Success(Term.Tuple(Term.I(1),new Cons(Term.I(2),Term.A("tail"))))
        ),
        new("maybe-map-pattern", "case a of K -> maybe #{K := V} ?= #{a => 42},V end end", Success(Term.I(42))),
        new(
            "maybe-bit-pattern",
            "maybe <<N,X:N,Rest/bitstring>> ?= <<3,5:3,2:2>>,{X,bit_size(Rest)} end",
            Success(Term.Tuple(Term.I(5),Term.I(2)))
        ),
        new("maybe-pattern-expression-failure", "maybe #{hd(atom) := X} ?= #{} else _ -> no end", Success(Term.A("no"))),
        new(
            "maybe-nested-independent-bindings",
            "maybe X=42,R=maybe X ?= 42,X end,{X,R} end",
            Success(Term.Tuple(Term.I(42),Term.I(42)))
        ),
        new("maybe-rhs-binds-before-pattern", "maybe X ?= (X=42),X end", Success(Term.I(42))),
        new(
            "maybe-else-captures-incoming",
            "case 42 of X -> maybe ok ?= wrong else _ -> (fun() -> X end)() end end",
            Success(Term.I(42))
        ),
        new(
            "maybe-success-effect-order",
            "begin put(mark,none),R=maybe {ok,X} ?= begin put(mark,rhs),{ok,42} end,put(mark,body),X else _ -> put(mark,else_body) end,{R,get(mark)} end",
            Success(Term.Tuple(Term.I(42),Term.A("body")))
        ),
        new(
            "maybe-mismatch-effect-order",
            "begin put(mark,none),R=maybe ok ?= begin put(mark,rhs),wrong end,put(mark,unreachable) else V -> {V,get(mark)} end,{R,get(mark)} end",
            Success(Term.Tuple(Term.Tuple(Term.A("wrong"),Term.A("rhs")),Term.A("rhs")))
        )
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_maybe_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).ToArray();
}
