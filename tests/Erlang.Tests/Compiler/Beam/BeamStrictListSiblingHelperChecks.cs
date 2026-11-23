using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStrictListSiblingHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-strict-list-generators-with-ordered-siblings";
    private const string Fixture = "beam_strict_list_sibling_helpers.erl";
    private const string TupleStrict = "-tuple_order/1-lc$^1/1-0-";
    private const string SequenceStrict = "-sequence_order/1-lc$^0/1-0-";
    private const string WrongSchedule = "Strict siblings lost their Core identity, SSA identity, or function order.";
    private const string WrongResult = "Strict siblings changed traversal, failure, source attribution, or effect order.";

    private const string Source = """
        -module(beam_strict_list_sibling_helpers).
        -export([tuple_order/1, sequence_order/1, guarded/1, closure/1, reset/0, events/0]).
        tuple_order(Values) ->
            {[remember(left,V) || {tag,V} <:- Values],
             [remember(right,X) || X <- Values]}.
        sequence_order(Values) ->
            First = [remember(left,V) || {tag,V} <:- Values],
            Second = [remember(right,X) || X <- Values],
            {First, Second}.
        guarded(Values) ->
            {[V || {tag,V} <:- Values, is_integer(V)], [X || X <- Values]}.
        closure(Values) ->
            Identity = fun(Item) -> Item end,
            {[V || {tag,V} <:- Values], [Identity(X) || X <- Values]}.
        reset() -> put(events, []), ok.
        events() -> get(events).
        remember(Which, Value) ->
            put(events, [{Which, Value}|get(events)]),
            Value.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ((IEnumerable)Property(Code(beam), "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (string)Property(function, "Name")).ToArray();
        if (!Helpers("tuple_order").SequenceEqual(["-tuple_order/1-lc$^0/1-1-", TupleStrict])
            || !Helpers("sequence_order").SequenceEqual(["-sequence_order/1-lc$^1/1-1-", SequenceStrict])
            || Helpers("guarded").Any() || Helpers("closure").Any())
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term bad = Term.A("bad");
            Term tail = Term.A("improper");
            Term first = Term.Tuple(Term.A("tag"), one);
            Term second = Term.Tuple(Term.A("tag"), two);
            Term input = Term.List(first, second);
            Term expected = Term.Tuple(Term.List(one, two), input);
            Term events = Term.List(
                Event("right", second),
                Event("right", first),
                Event("left", two),
                Event("left", one)
            );

            foreach (string function in new[] { "tuple_order", "sequence_order" })
            {
                await module.Invoke(context, "reset");
                RequireEqual(expected, await module.Invoke(context, function, input));
                RequireEqual(events, await module.Invoke(context, "events"));
            }

            var taggedBad = Term.Tuple(Term.A("tag"), bad);
            Term guardInput = Term.List(taggedBad, first);
            RequireEqual(Term.Tuple(Term.List(one), guardInput), await module.Invoke(context, "guarded", guardInput));
            RequireEqual(expected, await module.Invoke(context, "closure", input));
            await AssertStrictFailure(
                "tuple_order",
                Term.List(first, bad, second),
                Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), bad),
                TupleStrict,
                4
            );
            await AssertStrictFailure(
                "sequence_order",
                new Cons(first, tail),
                Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), tail),
                SequenceStrict,
                7
            );

            return Nil.Value;

            async ValueTask AssertStrictFailure(
                string function,
                Term value,
                Term reason,
                string helper,
                int line
            )
            {
                await module.Invoke(context, "reset");
                try
                {
                    await module.Invoke(context, function, value);
                }
                catch (ErlangException error) when (reason.Equals(error.Reason))
                {
                    if (error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom name, _, var location] } }
                        && name.Name == helper
                        && Cons.Items(location).Any(item => item.Equals(Term.Tuple(Term.A("line"), Term.I(line)))))
                    {
                        RequireEqual(Term.List(Event("left", one)), await module.Invoke(context, "events"));

                        return;
                    }
                }

                throw new InvalidOperationException(WrongResult + " " + function);
            }
        });

        IEnumerable<string> Helpers(string function) =>
            functions.Where(name => name.StartsWith("-" + function + "/1-lc$^", StringComparison.Ordinal));
    }

    private static Term Event(string side, Term item) => Term.Tuple(Term.A(side), item);

    private static void RequireEqual(Term expected, Term actual)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(WrongResult + " " + actual);
        }
    }
}
