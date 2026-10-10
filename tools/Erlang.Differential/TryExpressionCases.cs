namespace Erlang.Differential;

public static class TryExpressionCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Failure(string exceptionClass, Term reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A(exceptionClass), reason);

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("try-success-after", "try 42 after ignored end", Success(Term.I(42))),
        new("try-success-catch", "try ok catch _ -> unexpected end", Success(Term.A("ok"))),
        new(
            "try-of-body-bindings",
            "try X=41,X+1 of Y -> {X,Y} catch _ -> unexpected end",
            Success(Term.Tuple(Term.I(41),Term.I(42)))
        ),
        new("try-default-throw", "try throw({tag,7}) catch {tag,N} -> N end", Success(Term.I(7))),
        new("try-error-pattern", "try error({tag,7}) catch error:{tag,N} -> N end", Success(Term.I(7))),
        new("try-exit-class", "try exit(stopped) catch exit:R -> R end", Success(Term.A("stopped"))),
        new("try-class-variable", "try throw(reason) catch C:R -> {C,R} end", Success(Term.Tuple(Term.A("throw"),Term.A("reason")))),
        new("try-default-does-not-catch-error", "try error(reason) catch _ -> unexpected end", Failure("error",Term.A("reason"))),
        new("try-class-mismatch", "try throw(reason) catch error:_ -> unexpected end", Failure("throw",Term.A("reason"))),
        new(
            "try-stack-body",
            "try error(reason) catch error:R:S -> {R,is_list(S)} end",
            Success(Term.Tuple(Term.A("reason"),Term.A("true")))
        ),
        new("try-guard-alternatives", "try throw(2) catch N when N==1; N==2 -> yes; _ -> no end", Success(Term.A("yes"))),
        new("try-guard-failure-fallback", "try throw(2) catch N when hd(N)==1 -> no; N when N==2 -> yes end", Success(Term.A("yes"))),
        new("try-guard-rethrow", "try throw(2) catch N when N==1 -> no end", Failure("throw",Term.I(2))),
        new("try-of-guard-fallback", "try 2 of N when N==1 -> no; N -> N+1 after ignored end", Success(Term.I(3))),
        new(
            "try-of-no-match",
            "try 2 of 1 -> no catch error:_ -> unexpected end",
            Failure("error",Term.Tuple(Term.A("try_clause"),Term.I(2)))
        ),
        new(
            "try-of-error-outside-catch",
            "try ok of ok -> error(of_body) catch error:_ -> unexpected end",
            Failure("error",Term.A("of_body"))
        ),
        new(
            "try-handler-error-outside-catch",
            "try throw(initial) catch _ -> error(handler_body) end",
            Failure("error",Term.A("handler_body"))
        ),
        new(
            "try-after-success-effect",
            "begin put(mark,none),R=try 42 after put(mark,after_body) end,{R,get(mark)} end",
            Success(Term.Tuple(Term.I(42),Term.A("after_body")))
        ),
        new(
            "try-after-handler-effect",
            "begin put(mark,none),R=try throw(initial) catch _ -> put(mark,handler),7 after put(mark,after_body) end,{R,get(mark)} end",
            Success(Term.Tuple(Term.I(7),Term.A("after_body")))
        ),
        new(
            "try-after-of-failure-effect",
            "begin put(mark,none),R=try try ok of ok -> error(of_body) catch error:_ -> unexpected after put(mark,after_body) end catch error:E -> E end,{R,get(mark)} end",
            Success(Term.Tuple(Term.A("of_body"),Term.A("after_body")))
        ),
        new(
            "try-after-rethrow-effect",
            "begin put(mark,none),R=try try throw(initial) catch error:_ -> unexpected after put(mark,after_body) end catch E -> E end,{R,get(mark)} end",
            Success(Term.Tuple(Term.A("initial"),Term.A("after_body")))
        ),
        new("try-after-overrides-value", "try 42 after throw(after_body) end", Failure("throw",Term.A("after_body"))),
        new("try-after-overrides-error", "try error(initial) after exit(after_body) end", Failure("exit",Term.A("after_body"))),
        new(
            "try-after-overrides-handler",
            "try throw(initial) catch _ -> error(handler_body) after error(after_body) end",
            Failure("error",Term.A("after_body"))
        ),
        new(
            "try-incoming-binding",
            "begin A=9,try throw(2) catch N -> {A,N} after put(mark,A) end end",
            Success(Term.Tuple(Term.I(9),Term.I(2)))
        ),
        new("try-after-local-binding", "try 42 after A=9,put(mark,A) end", Success(Term.I(42)))
    ];
}
