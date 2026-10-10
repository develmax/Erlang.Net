namespace Erlang.Differential;

public static class TryModuleCases
{
    public static IReadOnlyList<CompiledModuleCase> All { get; } = TryExpressionCases.All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_try_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).Concat([
        new CompiledModuleCase(
            "compiled-try-local-call-protected",
            "-module(oracle_try_call). -export([run/0]). run()->try fail() catch error:R -> R end. fail()->error(local_body).",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.A("local_body"))
        ),
        new CompiledModuleCase(
            "compiled-try-of-local-call-unprotected",
            "-module(oracle_try_of_call). -export([run/0]). run()->try ok of ok -> fail() catch error:_ -> unexpected end. fail()->error(local_of).",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.A("local_of"))
        ),
        new CompiledModuleCase(
            "compiled-try-after-local-call-order",
            "-module(oracle_try_after_call). -export([run/0]). run()->put(mark,none),R=try work() after put(mark,after_body) end,{R,get(mark)}. work()->put(mark,work),42.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(42),Term.A("after_body")))
        )
    ]).ToArray();
}
