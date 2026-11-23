using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedListSiblingGeneratorChecks
{
    internal const string TestName = "compiler/beam/orders-nested-list-generator-siblings";
    private const string Module = "beam_nested_sibling_generators";
    private const string Ordering = "Nested list helpers must keep both ordinals and stay adjacent in the emitted order.";
    private const string Outputs = "Sequential and structurally different nested generators must preserve their values.";

    private const string Source = """
        -module(beam_nested_sibling_generators).
        -export([sequential/1, partial/2, tagged/2, nested_tagged/2, selected/4]).

        sequential(Input) ->
            Rows = [X || X <- Input, is_tuple(X)],
            Marked = lists:reverse([Mark || {Mark, _} <- Rows]),
            Expanded = [{Mark, V} || {Mark, Values} <- Rows, V <- Values],
            {Marked, Expanded}.

        partial(Input, Fallback) ->
            A = lists:reverse([{Key, V} || {Key, Values} <- Input, V <- Values]),
            B = [{K, V} || {K, {tag, Values}} <- Fallback, V <- Values],
            C = [{X, X} || X <- B],
            {A, B, C}.

        tagged(Input, Stamp) ->
            A = [{Anno, Value} || {attribute, Anno, kind, Values} <- Input, Value <- Values],
            B = [{Stamp, X} || X <- A],
            {A, B}.

        nested_tagged(Forms, Stamp) ->
            A = [{{F, A}, Module} ||
                    {attribute, _, import, {Module, Names}} <- Forms,
                    {F, A} <- Names],
            B = [{Stamp, X} || X <- A],
            {A, B}.

        selected(Selector, Left, Right, Context) ->
            Result = case Selector =:= [] of
                true -> [Value || Item <- lists:reverse(Left), Value <- select_inner(Item, Context)];
                false -> [Value || Item <- Right, Value <- select_inner(Item, Context)]
            end,
            {Selector, Result}.

        select_inner(first, Values) -> Values;
        select_inner(second, Values) -> [second | Values];
        select_inner(_, _) -> bad_inner.
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
                ("-selected/4-lc$^0/1-0-", 2),
                ("-selected/4-lc$^1/1-1-", 3),
                ("-selected/4-lc$^2/1-2-", 2),
                ("-selected/4-lc$^3/1-3-", 3),
                ("-nested_tagged/2-lc$^2/1-2-", 2),
                ("-nested_tagged/2-lc$^0/1-0-", 1),
                ("-nested_tagged/2-lc$^1/1-1-", 3),
                ("-tagged/2-lc$^2/1-2-", 2),
                ("-tagged/2-lc$^0/1-0-", 1),
                ("-tagged/2-lc$^1/1-1-", 3),
                ("-partial/2-lc$^4/1-4-", 1),
                ("-partial/2-lc$^0/1-0-", 1),
                ("-partial/2-lc$^1/1-1-", 3),
                ("-sequential/1-lc$^2/1-2-", 1),
                ("-sequential/1-lc$^3/1-3-", 3),
                ("-sequential/1-lc$^1/1-1-", 1),
                ("-sequential/1-lc$^0/1-0-", 1)
            ]))
        {
            throw new InvalidOperationException(Ordering);
        }

        await using var runtime = new ProcessRuntime();
        BeamModule image = BeamModule.Prepare(beam, new BeamDependencySelection(["lists", ErlangModuleNames.Module]));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term x = Term.A("x");
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term four = Term.I(4);
            Term valid = Term.List(Term.Tuple(a, Term.List(one, two)), Term.A("skip"), Term.Tuple(b, Nil.Value));
            Term sequential = await image.Invoke(context, "sequential", valid);
            Term nested = Term.List(Term.A("skip"), Term.Tuple(x, Term.Tuple(Term.A("tag"), Term.List(four))));
            Term partial = await image.Invoke(
                context,
                "partial",
                valid,
                nested
            );
            Term hit = Term.Tuple(x, four);
            Term expected = Term.Tuple(Term.List(Term.Tuple(a, two), Term.Tuple(a, one)), Term.List(hit), Term.List(Term.Tuple(hit, hit)));
            if (!sequential.Equals(Term.Tuple(Term.List(b, a), Term.List(Term.Tuple(a, one), Term.Tuple(a, two))))
                || !partial.Equals(expected))
            {
                throw new InvalidOperationException(Outputs);
            }

            Term attribute = Term.A("attribute");
            Term kind = Term.A("kind");
            Term import = Term.A("import");
            Term taggedRows = Term.List(
                Term.A("skip"),
                Term.Tuple(
                    attribute,
                    a,
                    kind,
                    Term.List(one, two)
                )
            );
            Term tagged = await image.Invoke(
                context,
                "tagged",
                taggedRows,
                x
            );
            Term first = Term.Tuple(a, one);
            Term second = Term.Tuple(a, two);
            if (!tagged.Equals(Term.Tuple(
                    Term.List(first, second),
                    Term.List(Term.Tuple(x, first), Term.Tuple(x, second))
                )))
            {
                throw new InvalidOperationException(Outputs);
            }

            Term imported = Term.Tuple(
                attribute,
                b,
                import,
                Term.Tuple(a, Term.List(Term.A("skip"), Term.Tuple(x, four)))
            );
            Term nestedTagged = await image.Invoke(
                context,
                "nested_tagged",
                Term.List(imported),
                b
            );
            Term importedValue = Term.Tuple(Term.Tuple(x, four), a);
            if (!nestedTagged.Equals(Term.Tuple(Term.List(importedValue), Term.List(Term.Tuple(b, importedValue)))))
            {
                throw new InvalidOperationException(Outputs);
            }

            Term firstKey = Term.A("first");
            Term secondKey = Term.A("second");
            Term selectedTrue = await image.Invoke(
                context,
                "selected",
                Nil.Value,
                Term.List(firstKey),
                b,
                Term.List(a)
            );
            Term selectedFalse = await image.Invoke(
                context,
                "selected",
                x,
                b,
                Term.List(firstKey, secondKey),
                Term.List(a)
            );
            if (!selectedTrue.Equals(Term.Tuple(Nil.Value, Term.List(a)))
                || !selectedFalse.Equals(Term.Tuple(x, Term.List(a, secondKey, a))))
            {
                throw new InvalidOperationException(Outputs);
            }

            bool rejectedInner = false;
            try
            {
                await image.Invoke(
                    context,
                    "selected",
                    x,
                    Nil.Value,
                    Term.List(b),
                    Nil.Value
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("bad_generator"), Term.A("bad_inner"))))
            {
                rejectedInner = true;
            }

            if (!rejectedInner)
            {
                throw new InvalidOperationException(Outputs);
            }

            try
            {
                Term invalid = Term.Tuple(
                    attribute,
                    a,
                    kind,
                    b
                );
                await image.Invoke(
                    context,
                    "tagged",
                    Term.List(invalid),
                    x
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("bad_generator"), b)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(Outputs);
        });
    }
}
