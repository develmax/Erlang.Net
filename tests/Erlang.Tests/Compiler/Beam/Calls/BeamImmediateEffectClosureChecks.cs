using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamImmediateEffectClosureChecks
{
    internal const string TestName = "compiler/beam/inlines-isolated-immediate-effect-closures";
    private const string ShapeError = "Only immediate anonymous effects with isolated bindings can be inlined.";
    private const string Source = """
        -module(immediate_effect_closures).
        -export([direct/1,collision/1,returned/1,indirect/1]).
        direct(Input) ->
            fun() -> Local = Input, retain(Local) end(),
            kept.
        collision(Outer) ->
            fun() -> Outer = 1, retain(Outer) end(),
            kept.
        returned(Input) ->
            fun() -> retain(Input) end.
        indirect(Input) ->
            F = fun() -> retain(Input) end,
            F(),
            kept.
        retain(X) -> put(value, X).
        """;

    internal static void Run()
    {
        ModuleDefinition source = new Parser(Source).ParseModule();
        var result = BeamImmediateEffectClosures.Inline(source.DeclaredFunctions);
        var functions = result.ToDictionary(function => function.Name, StringComparer.Ordinal);
        if (ContainsClosure("direct") || !ContainsClosure("collision") || !ContainsClosure("returned")
            || !ContainsClosure("indirect") || functions["direct"].Clauses[0].Body is not
            Expr.Sequence { Items: [Expr.Block, Expr.Literal] })
        {
            throw new InvalidOperationException(ShapeError);
        }

        bool ContainsClosure(string name) => LocalFunctionEntries.Walk(functions[name].Clauses[0].Body)
            .Any(expression => expression is Expr.Fun);
    }
}
