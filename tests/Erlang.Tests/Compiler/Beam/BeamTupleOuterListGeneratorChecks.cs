using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleOuterListGeneratorChecks
{
    internal const string TestName = "compiler/beam/matches-tuple-outer-list-generators";
    private const string Module = "beam_tuple_outer_generators";
    private const string Definitions = "Tuple generator helpers must follow the pinned physical order and capture arity.";
    private const string Results = "Nested tuple generators must preserve outer skips, inner exhaustion and repeated bindings.";

    private const string Source = """
        -module(beam_tuple_outer_generators).
        -export([grouped/1, captured/2, computed/1, repeated/1, mark/2]).

        grouped(Input) ->
            [{Tag, [V]} || {Tag, Values} <- Input, V <- Values].

        captured(Tag, Input) ->
            [{Tag, V} || {_Unused, Values} <- Input, V <- Values].

        computed(Input) ->
            [V || {Tag, Seed} <- ?MODULE:mark(outer, Input),
                  V <- ?MODULE:mark(Tag, Seed)].

        repeated(Input) ->
            [V || {Values, Values} <- Input, V <- Values].

        mark(Event, Value) ->
            self() ! Event,
            Value.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        var helpers = ((IEnumerable)Property(Code(beam), "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .Where(function => function.Name.Contains("-lc$^", StringComparison.Ordinal))
            .ToArray();
        if (!helpers.SequenceEqual([
                ("-computed/1-lc$^0/1-0-", 1),
                ("-computed/1-lc$^1/1-1-", 2),
                ("-captured/2-lc$^0/1-0-", 2),
                ("-captured/2-lc$^1/1-1-", 3),
                ("-grouped/1-lc$^0/1-0-", 1),
                ("-grouped/1-lc$^1/1-1-", 3)
            ]))
        {
            throw new InvalidOperationException(Definitions);
        }

        await using var runtime = new ProcessRuntime();
        BeamModule image = BeamModule.Prepare(beam, new BeamDependencySelection([ErlangModuleNames.Module]));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term three = Term.I(3);
            Term input = Term.List(
                Term.Tuple(a, Term.List(one, two)),
                Term.A("skip"),
                Term.Tuple(a, Nil.Value),
                Term.Tuple(b, Term.List(three))
            );
            Term grouped = await image.Invoke(context, "grouped", input);
            Term captured = await image.Invoke(
                context,
                "captured",
                a,
                input
            );
            Term repeated = await image.Invoke(
                context,
                "repeated",
                Term.List(Term.Tuple(Term.List(one), Term.List(one)), Term.Tuple(Term.List(two), Term.List(three)))
            );
            if (!grouped.Equals(Term.List(
                    Term.Tuple(a, Term.List(one)),
                    Term.Tuple(a, Term.List(two)),
                    Term.Tuple(b, Term.List(three))
                ))
                || !captured.Equals(Term.List(Term.Tuple(a, one), Term.Tuple(a, two), Term.Tuple(a, three)))
                || !repeated.Equals(Term.List(one)))
            {
                throw new InvalidOperationException(Results);
            }

            return Nil.Value;
        });
    }
}
