using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosureSiblingAppendChecks
{
    internal const string TestName = "compiler/beam/keeps-closure-and-fused-append-helper-ordinals";
    private const string Source = """
        -module(beam_closure_sibling_append).
        -export([probe/1, computed/1, supplied/2]).

        probe(Rows) ->
            put(events, []),
            Result = try computed(Rows)
                     catch error:Reason -> {failed, Reason}
                     end,
            {Result, lists:reverse(get(events))}.

        computed(Rows) ->
            Observer = fun({ok, X}) -> {observed, X}; (Anything) -> Anything end,
            pack(Observer, [note(Row) || Row <- Rows] ++ tail()).

        supplied(Rows, Tail) ->
            Observer = fun({ok, X}) -> {observed, X}; (Anything) -> Anything end,
            pack(Observer, [plain(Row) || Row <- Rows] ++ Tail).

        pack(Observer, Values) -> {Observer({ok, first}), Values}.
        plain(Row) -> Row.
        note(Row) -> put(events, [Row | get(events)]), Row.
        tail() -> put(events, [tail | get(events)]), [tail].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_closure_sibling_append.erl");
        object code = Code(image);
        var keys = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity")))
            .ToHashSet();
        if (!keys.Contains(("-computed/1-fun-0-", 1))
            || !keys.Contains(("-computed/1-lc$^1/1-0-", 2))
            || keys.Any(key => key.Item1.StartsWith("-supplied/2-lc$", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("The earlier closure and later fused append require distinct ordinal owners.");
        }

        var dependencies = new BeamDependencySelection([ErlangModuleNames.Module, "lists"]);
        BeamModule module = BeamModule.Prepare(image, dependencies);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term tail = Term.A("tail");
            Term observed = Term.Tuple(Term.A("observed"), first);
            Term expected = Term.Tuple(
                Term.Tuple(observed, Term.List(first, second, tail)),
                Term.List(tail, first, second)
            );
            Term invalid = Term.A("improper");
            Term failure = Term.Tuple(
                Term.Tuple(Term.A("failed"), Term.Tuple(Term.A("bad_generator"), invalid)),
                Term.List(tail, first)
            );
            if (!expected.Equals(await module.Invoke(context, "probe", [Term.List(first, second)]))
                || !failure.Equals(await module.Invoke(context, "probe", [new Cons(first, invalid)]))
                || !Term.Tuple(observed, Term.List(first, tail)).Equals(await module.Invoke(
                    context,
                    "supplied",
                    [Term.List(first), Term.List(tail)]
                )))
            {
                throw new InvalidOperationException("The helper must evaluate the tail before mapping or reporting a generator error.");
            }

            return Nil.Value;
        });
    }
}
