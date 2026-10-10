namespace Erlang.Differential;

public static class CatchPatternCases
{
    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new(
            "try-negative-integer-stack-pattern",
            "try error(-1) catch error:-1:S -> is_list(S) end",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.A("true"))
        ),
        new(
            "try-negative-float-stack-pattern",
            "try error(-1.5) catch error:-1.5:S -> is_list(S) end",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.A("true"))
        ),
        new(
            "try-parenthesized-negative-stack-pattern",
            "try error(-1) catch error:-(1):S -> is_list(S) end",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.A("true"))
        )
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_catch_signed_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).ToArray();
}
