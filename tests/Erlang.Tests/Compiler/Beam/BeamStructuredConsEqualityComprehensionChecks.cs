using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStructuredConsEqualityComprehensionChecks
{
    internal const string TestName = "compiler/beam/structured-cons-equality-comprehension";

    private const string Module = "beam_structured_cons_equality_comprehension";
    private const string Source = """
        -module(beam_structured_cons_equality_comprehension).
        -export([partition/2, bare/3, mixed/3, probe/2]).

        partition([Head|Left], [Head|Right]) ->
            partition(Left, Right);
        partition(Last = [_], []) ->
            Last;
        partition(Last = [_], Right) ->
            Prefix = [visit(single, Item) || Item <- Right],
            Prefix ++ Last;
        partition(Left, []) ->
            Left;
        partition(Left, Right) ->
            Prefix = [visit(other, Item) || Item <- Right],
            Prefix ++ Left.

        bare(Head, Head, Rows) ->
            [visit(same, Item) || Item <- Rows];
        bare(_, _, Rows) ->
            [visit(other, Item) || Item <- Rows].

        mixed(Head, [Head|_], Rows) ->
            [visit(same, Item) || Item <- Rows];
        mixed(_, _, Rows) ->
            [visit(other, Item) || Item <- Rows].

        probe(Left, Right) ->
            put(seen, []),
            Result = partition(Left, Right),
            {Result, lists:reverse(get(seen))}.

        visit(Route, Item) ->
            put(seen,[{Route,Item}|get(seen)]),
            {Route,Item}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity"))).ToArray();
        var partition = functions.Where(function => function.Item1.StartsWith("-partition/2-lc$^", StringComparison.Ordinal));
        if (!partition.SequenceEqual([("-partition/2-lc$^1/1-0-", 1), ("-partition/2-lc$^0/1-1-", 1)])
            || functions.Any(function => function.Item1.StartsWith("-bare/3-lc$^", StringComparison.Ordinal)
                || function.Item1.StartsWith("-mixed/3-lc$^", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Repeated cons head bindings must preserve the pinned comprehension order.");
        }

        await using var runtime = new ProcessRuntime();
        var dependencies = new BeamDependencySelection(["lists", ErlangModuleNames.Module]);
        BeamModule image = BeamModule.Prepare(beam, dependencies);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term same = Term.A("same");
            Term last = Term.A("last");
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term single = Term.A("single");
            Term other = Term.A("other");
            Term number = Term.I(1);
            Term floating = new FloatTerm(1.0);
            Term firstSeen = Term.Tuple(single, first);
            Term secondSeen = Term.Tuple(single, second);
            Term floatSeen = Term.Tuple(other, floating);
            Term otherSeen = Term.Tuple(other, second);
            await Probe(
                Term.List(same, last),
                Term.List(same),
                Term.List(last),
                Nil.Value
            );
            await Probe(
                Term.List(same, last),
                Term.List(same, first, second),
                Term.List(firstSeen, secondSeen, last),
                Term.List(firstSeen, secondSeen)
            );
            await Probe(
                Term.List(number, last),
                Term.List(floating, second),
                Term.List(
                    floatSeen,
                    otherSeen,
                    number,
                    last
                ),
                Term.List(floatSeen, otherSeen)
            );

            return Nil.Value;

            async Task Probe(
                Term left,
                Term right,
                Term result,
                Term seen
            )
            {
                if (!Term.Tuple(result, seen).Equals(await image.Invoke(
                    context,
                    "probe",
                    left,
                    right
                )))
                {
                    throw new InvalidOperationException("The repeated head must use exact equality and preserve the order of effects.");
                }
            }
        });
    }
}
