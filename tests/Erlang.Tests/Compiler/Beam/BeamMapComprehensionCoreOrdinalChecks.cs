using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamMapComprehensionCoreOrdinalChecks
{
    internal const string TestName = "compiler/beam/reserves-core-ordinals-for-map-generators";
    private const string IncorrectOrdinals = "A pair map generator must reserve its Core ordinal before following anonymous closures.";
    private const string Source = """
        -module(beam_map_core_ordinal).
        -export([build/1, ordinary/1, twice/2, after_fun/1, from_map/1]).

        build(Items) ->
            Table = #{K => V || {K,V} <:- Items},
            {Table, fun(X) -> {tag,X} end}.

        ordinary(Items) ->
            Table = maps:from_list(Items),
            {Table, fun(X) -> {tag,X} end}.

        twice(Left,Right) ->
            A = #{K => V || {K,V} <- Left},
            B = #{K => V || {K,V} <:- Right},
            {A,B,fun(X) -> {tag,X} end}.

        after_fun(Items) ->
            F = fun(X) -> {tag,X} end,
            {#{K => V || {K,V} <:- Items},F}.
        from_map(Items) ->
            Table = #{K => V || K := V <- Items},
            {Table, fun(X) -> {tag,X} end}.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_map_core_ordinal.erl");
        BeamModule module = BeamModule.Prepare(image);
        string[] closures = module.Functions.Where(function => function.Name.Contains("-fun-", StringComparison.Ordinal))
            .Select(function => function.Name).Order(StringComparer.Ordinal).ToArray();
        if (closures is not
            [
                "-after_fun/1-fun-0-",
                "-build/1-fun-1-",
                "-from_map/1-fun-1-",
                "-ordinary/1-fun-0-",
                "-twice/2-fun-2-"
            ])
        {
            throw new InvalidOperationException(IncorrectOrdinals);
        }

        return Task.CompletedTask;
    }
}
