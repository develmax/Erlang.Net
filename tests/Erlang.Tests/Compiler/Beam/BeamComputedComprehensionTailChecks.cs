using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamComputedComprehensionTailChecks
{
    internal const string TestName = "compiler/beam/keeps-a-computed-comprehension-tail-on-its-branch";
    private const string Fixture = "beam_computed_comprehension_tail.erl";
    private const string Helper = "-pick/2-lc$^0/1-0-";
    private const string Incorrect = "The selected comprehension must evaluate its tail once before consuming the generator.";

    private const string Source = """
        -module(beam_computed_comprehension_tail).
        -export([trace/2]).
        trace(Mode, Items) ->
            put(events, []),
            try pick(Mode, Items) of
                Result -> {Result, get(events)}
            catch
                error:Reason -> {error, Reason, get(events)}
            end.

        pick(Mode, Items) ->
            case Mode of
                skip -> skipped;
                Selected ->
                    Token = remember(seed),
                    [remember({Token, Item}) || Item <- Items] ++ finish(Selected)
            end.

        finish(Mode) ->
            remember(tail),
            case Mode of
                fail -> erlang:error(tail_failure);
                _ -> [tail]
            end.

        remember(Event) ->
            put(events, [Event | get(events)]),
            Event.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        var atoms = (IEnumerable)Property(Property(code, "Structure"), "Atoms");
        if (!atoms.Cast<string>().Contains(Helper, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(Incorrect);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term seed = Term.A("seed");
            Term tail = Term.A("tail");
            Term first = Term.Tuple(seed, Term.I(1));
            Term second = Term.Tuple(seed, Term.I(2));
            Term badSource = Term.A("bad_source");
            Term eventsBeforeGenerator = Term.List(tail, seed);
            Term expected = Term.Tuple(Term.List(first, second, tail), Term.List(
                second,
                first,
                tail,
                seed
            ));
            Term normal = await module.Invoke(
                context,
                "trace",
                Term.A("normal"),
                Term.List(Term.I(1), Term.I(2))
            );
            Term skipped = await module.Invoke(
                context,
                "trace",
                Term.A("skip"),
                badSource
            );
            Term generatorError = await module.Invoke(
                context,
                "trace",
                Term.A("normal"),
                badSource
            );
            Term tailError = await module.Invoke(
                context,
                "trace",
                Term.A("fail"),
                badSource
            );
            if (!normal.Equals(expected) || !skipped.Equals(Term.Tuple(Term.A("skipped"), Nil.Value))
                || !generatorError.Equals(Term.Tuple(
                    Term.A("error"),
                    Term.Tuple(Term.A("bad_generator"), badSource),
                    eventsBeforeGenerator
                ))
                || !tailError.Equals(Term.Tuple(Term.A("error"), Term.A("tail_failure"), eventsBeforeGenerator)))
            {
                throw new InvalidOperationException(Incorrect);
            }

            return Nil.Value;
        });
    }
}
