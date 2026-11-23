using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamMapCoreCallsChecks
{
    internal const string TestName = "compiler/beam/lowers-static-map-aliases";
    private const string Fixture = "beam_map_core_calls.erl";
    private const string WrongImage = "Static maps calls must have the same BEAM as the Core equivalents.";
    private const string WrongBinding = "Only a statically resolved maps call can be lowered.";
    private const string Source = """
        -module(beam_map_core_calls).
        -export([value/2, contains/2, count/1, empty/0, default_field/0]).
        -record(box, {items = maps:new()}).
        value(K, M) -> maps:get(K, M).
        contains(K, M) -> maps:is_key(K, M).
        count(M) -> maps:size(M).
        empty() -> maps:new().
        default_field() -> #box{}.
        """;

    internal static Task Run()
    {
        string core = Source.Replace("maps:get", "erlang:map_get", StringComparison.Ordinal)
            .Replace("maps:is_key", "erlang:is_map_key", StringComparison.Ordinal)
            .Replace("maps:size", "erlang:map_size", StringComparison.Ordinal)
            .Replace("maps:new()", "#{}", StringComparison.Ordinal);
        if (!CodeGeneration.CompileBeamModule(Source, Fixture).SequenceEqual(CodeGeneration.CompileBeamModule(core, Fixture)))
        {
            throw new InvalidOperationException(WrongImage);
        }

        Expr[] arguments = [new Expr.Variable("K"), new Expr.Variable("M")];
        var bindings = new SourceCallBindings(false, [], [("maps", "get", arguments.Length)]);
        var imported = new Expr.Call(null, "get", arguments) { SourceLine = 19 };
        var dynamic = new Expr.Call("maps", "get", arguments) { DynamicRemote = true };
        var other = new Expr.Call("other", "get", arguments);
        var functions = new FunctionDefinition[]
        {
            new("lookup", arguments.Length, [new Clause([], null, new Expr.Tuple([imported, dynamic, other]))])
        };
        var result = (Expr.Tuple)BeamMapCoreCalls.Rewrite(functions, bindings)[0].Clauses[0].Body;
        if (result.Items[0] is not Expr.Call { Module: "erlang", Function: "map_get", SourceLine: 19 } converted
            || !converted.Arguments.SequenceEqual(arguments)
            || result.Items[1] is not Expr.Call { DynamicRemote: true, Module: "maps", Function: "get" }
            || result.Items[2] is not Expr.Call { Module: "other", Function: "get" })
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }
}
