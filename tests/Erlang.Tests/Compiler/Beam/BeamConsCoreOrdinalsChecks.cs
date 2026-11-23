using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamConsCoreOrdinalsChecks
{
    internal const string TestName = "compiler/beam/allocates-core-lambda-identities-from-cons-tail";
    private const string IncorrectOrdinals = "Core traversal must reserve constructor and call arguments in the order used by v3_core.";
    private const string Source = """
        -module(beam_cons_ordinal).
        -export([cons/1,seq/1,tuple/1,arguments/1,apply_order/1,operators/1,short/1,bits/1,combine/2,run/1]).
        cons(Input) ->
            [apply(fun(X) -> X end, [Input]) |
             lists:map(fun(X) -> X + 1 end, [Input])].
        seq(Input) ->
            apply(fun(X) -> X end, [Input]),
            apply(fun(X) -> X + 1 end, [Input]).
        tuple(Input) ->
            {fun() -> Input end, fun() -> Input + 1 end}.
        arguments(Input) ->
            combine(fun() -> Input end, fun() -> Input + 1 end).
        apply_order(Input) ->
            (fun(First, Second) -> {First, Second} end)(fun() -> Input end, fun() -> Input + 1 end).
        operators(Input) ->
            run(fun() -> Input end) + run(fun() -> 1 end).
        short(Input) ->
            run(fun() -> false end) andalso run(fun() -> Input end).
        bits(Input) ->
            << (run(fun() -> Input end)) : (run(fun() -> 4 end)),
               (run(fun() -> Input + 1 end)) : (run(fun() -> 4 end)) >>.
        combine(First, Second) ->
            {First, Second}.
        run(Fun) ->
            Fun().
        """;

    internal static Task Run()
    {
        var functions = new Parser(Source).ParseModule().DeclaredFunctions;
        if (!ReadOrdinals(functions[0]).SequenceEqual(new int?[] { 1, 0 })
            || !ReadOrdinals(functions[1]).SequenceEqual(new int?[] { 0, 1 })
            || !ReadOrdinals(functions[2]).SequenceEqual(new int?[] { 1, 0 })
            || !ReadOrdinals(functions[3]).SequenceEqual(new int?[] { 1, 0 })
            || !ReadOrdinals(functions[4]).SequenceEqual(new int?[] { 0, 2, 1 })
            || !ReadOrdinals(functions[5]).SequenceEqual(new int?[] { 1, 0 })
            || !ReadOrdinals(functions[6]).SequenceEqual(new int?[] { 0, 1 })
            || !ReadOrdinals(functions[7]).SequenceEqual(new int?[] { 2, 3, 0, 1 }))
        {
            throw new InvalidOperationException(IncorrectOrdinals);
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<int?> ReadOrdinals(FunctionDefinition function) =>
        LocalFunctionEntries.Walk(function.Clauses.Single().Body).OfType<Expr.Fun>().Select(closure => closure.CoreOrdinal);
}
