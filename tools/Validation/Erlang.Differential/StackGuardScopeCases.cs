namespace Erlang.Differential;

public static class StackGuardScopeCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("stack-body-if-guard", "try error(reason) catch error:_:S -> if is_list(S) -> yes end end", Success(Term.A("yes"))),
        new(
            "stack-body-case-guard",
            "try error(reason) catch error:_:S -> case ok of ok when is_list(S) -> yes; _ -> no end end",
            Success(Term.A("yes"))
        ),
        new(
            "stack-captured-fun-guard",
            "try error(reason) catch error:_:S -> (fun() -> if is_list(S) -> yes end end)() end",
            Success(Term.A("yes"))
        ),
        new(
            "stack-shadowed-fun-guard",
            "try error(reason) catch error:_:S -> (fun(S) when is_integer(S) -> S end)(7) end",
            Success(Term.I(7))
        ),
        new(
            "stack-nested-catch-guard",
            "try error(reason) catch error:_:S -> try throw(inner) catch R when is_list(S) -> R end end",
            Success(Term.A("inner"))
        ),
        new(
            "stack-body-receive-guard",
            "try error(reason) catch error:_:S -> self()!ok,receive ok when is_list(S) -> yes after 0 -> no end end",
            Success(Term.A("yes"))
        )
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_stack_scope_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).ToArray();
}
