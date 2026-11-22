using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDominatingHelperConstantChecks
{
    internal const string TestName = "compiler/beam/substitutes-dominating-helper-constants";
    private const string Fixture = "beam_dominating_helper_constants.erl";
    private const string WrongArity = "A source-constant binding was retained as a lifted helper argument.";
    private const string WrongResult = "Helper constant substitution changed evaluation order, scope, or result.";

    private const string Source = """
        -module(beam_dominating_helper_constants).
        -export([lookup/1, events/0, invoke_closure/2, invoke_shadow/2, invoke_repeated/3, checked/2, shadow_generator/0, sibling_constants/1]).
        -record(row, {name, count}).
        lookup(Rows) ->
            Fields = record_info(fields, row),
            remember(lookup),
            [decorate(Fields, Whole) || Whole = #row{} <- Rows].
        events() -> get(helper_events).
        remember(Value) ->
            Before = case get(helper_events) of undefined -> []; Found -> Found end,
            put(helper_events, [Value | Before]),
            ok.
        decorate(Fields, Row) -> {Fields, Row}.
        closure(Outer) ->
            Tag = {fixed,2},
            remember(closure),
            fun(Item) -> {Tag,Outer,Item} end.
        invoke_closure(Outer, Item) ->
            F = closure(Outer),
            F(Item).
        shadow(Outer) ->
            Tag = fixed,
            fun
                (Tag) when is_integer(Tag) -> {Tag,Outer};
                (Item) -> {Tag,Outer,Item}
            end.
        invoke_shadow(Outer, Item) ->
            F = shadow(Outer),
            F(Item).
        repeated(Outer) ->
            Tag = fixed,
            fun
                (Tag, Tag) -> {Tag,Outer};
                (Left, Right) -> {Tag,Outer,Left,Right}
            end.
        invoke_repeated(Outer, Left, Right) ->
            F = repeated(Outer),
            F(Left, Right).
        checked(Expected, Rows) ->
            Expected = fixed,
            [decorate(Expected, Item) || Item <- Rows].
        sibling_constants(Caller) ->
            Base = [read],
            [A, B] = [remember_item(first, V) || V <- [one, two]],
            Options = [public | Base],
            [C, D] = [remember_item(Options, V) || V <- [three, four]],
            [E, F] = [remember_item(Caller, V) || V <- [five, six]],
            {A, B, C, D, E, F}.
        remember_item(Tag, Item) ->
            Value = {Tag, Item},
            remember(Value),
            Value.
        shadow_generator() ->
            Source = [one,two],
            [Source || Source <- Source, is_atom(Source)].
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        var helpers = ((IEnumerable)Property(Code(beam), "Functions")).Cast<object>()
            .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity"))).ToHashSet();
        if (!helpers.Contains(("-lookup/1-lc$^0/1-0-", 1)) || !helpers.Contains(("-closure/1-fun-0-", 2))
            || !helpers.Contains(("-sibling_constants/1-lc$^0/1-0-", 1))
            || !helpers.Contains(("-sibling_constants/1-lc$^1/1-1-", 1))
            || !helpers.Contains(("-sibling_constants/1-lc$^2/1-2-", 2)))
        {
            throw new InvalidOperationException(WrongArity);
        }

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term fixedValue = Term.A("fixed");
            Term outer = Term.A("outer");
            Term item = Term.A("item");
            Term two = Term.I(2);
            Term row = Term.Tuple(Term.A("row"), item, two);
            Term fields = Term.List(Term.A("name"), Term.A("count"));
            Require(
                Term.List(Term.Tuple(fields, row)),
                await module.Invoke(context, "lookup", Term.List(item, row))
            );
            Require(
                Term.Tuple(Term.Tuple(fixedValue, two), outer, item),
                await module.Invoke(
                    context,
                    "invoke_closure",
                    outer,
                    item
                )
            );
            Require(Term.List(Term.A("closure"), Term.A("lookup")), await module.Invoke(context, "events"));
            Require(Term.Tuple(two, outer), await module.Invoke(
                context,
                "invoke_shadow",
                outer,
                two
            ));
            Require(Term.Tuple(fixedValue, outer, item), await module.Invoke(
                context,
                "invoke_shadow",
                outer,
                item
            ));
            Require(Term.Tuple(two, outer), await module.Invoke(
                context,
                "invoke_repeated",
                outer,
                two,
                two
            ));
            Require(Term.Tuple(
                fixedValue,
                outer,
                two,
                item
            ), await module.Invoke(
                context,
                "invoke_repeated",
                outer,
                two,
                item
            ));
            Require(Term.List(Term.A("one"), Term.A("two")), await module.Invoke(context, "shadow_generator"));
            Require(
                Term.List(Term.Tuple(fixedValue, item)),
                await module.Invoke(
                    context,
                    "checked",
                    fixedValue,
                    Term.List(item)
                )
            );

            Term options = Term.List(Term.A("public"), Term.A("read"));
            Term first = Term.Tuple(Term.A("first"), Term.A("one"));
            Term second = Term.Tuple(Term.A("first"), Term.A("two"));
            Term third = Term.Tuple(options, Term.A("three"));
            Term fourth = Term.Tuple(options, Term.A("four"));
            Term fifth = Term.Tuple(outer, Term.A("five"));
            Term sixth = Term.Tuple(outer, Term.A("six"));
            Require(
                Term.Tuple(
                    first,
                    second,
                    third,
                    fourth,
                    fifth,
                    sixth
                ),
                await module.Invoke(context, "sibling_constants", outer)
            );
            Require(
                Term.List(
                    sixth,
                    fifth,
                    fourth,
                    third,
                    second,
                    first,
                    Term.A("closure"),
                    Term.A("lookup")
                ),
                await module.Invoke(context, "events")
            );

            try
            {
                await module.Invoke(
                    context,
                    "checked",
                    outer,
                    Term.List(item)
                );
            }
            catch (ErlangException error) when (Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), fixedValue).Equals(error.Reason))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void Require(Term expected, Term actual)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(WrongResult);
        }
    }
}
