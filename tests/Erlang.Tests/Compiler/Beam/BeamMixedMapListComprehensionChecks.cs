using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamMixedMapListComprehensionChecks
{
    internal const string TestName = "compiler/beam/reserves-a-map-generator-before-a-list-helper";
    private const string Fixture = "beam_mixed_map_list_comprehension.erl";
    private const string Lists = "lists";
    private const string Maps = "maps";
    private const string MissingHelper = "The map and list helpers must occupy their respective Core and SSA positions.";
    private const string WrongResult = "The mixed comprehensions changed the captured value, order, or generator failure.";

    private const string Source = """
        -module(beam_mixed_map_list_comprehension).
        -export([before/2, call_operand/2, crossed/2, inline_bound/2, inline_prefix_call/2]).

        before(Inputs, Stamp) ->
            {Rows, Values} = Inputs,
            Index = #{K => {V, V} || {tag, K, V} <:- Rows},
            {[render(Item, Stamp) || Item <- Values], Index}.

        call_operand(Keys, Stamp) ->
            Index = keep(#{K => [] || K <- Keys}),
            {Index, [render(K, Stamp) || K <- Keys]}.

        crossed(Keys, Stamp) ->
            {[render(Key, Stamp) || Key <- Keys], #{Key => Key || Key <- Keys}}.

        inline_bound(Inputs, Stamp) ->
            {Rows, Values} = Inputs,
            Index = #{K => {V, V} || {tag, K, V} <:- Rows},
            Rendered = [render_index(Item, Index) || Item <- Values],
            {Rendered, Stamp}.

        inline_prefix_call(Inputs, Stamp) ->
            Hash = erlang:phash2(Stamp),
            {Rows, Values} = Inputs,
            Index = #{K => {V, V} || {tag, K, V} <:- Rows},
            Rendered = [render_index(Item, Index) || Item <- Values],
            {Rendered, Hash}.

        render_index({wrapped, Key}, Index) -> render_index(Key, Index);
        render_index(Key, Index) -> {Key, map_get(Key, Index)}.
        keep(Value) -> Value.
        render(Item, Stamp) -> {Stamp, Item}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([Lists, Maps]));
        string[] names = module.Functions.Select(function => function.Name).ToArray();
        if (!names.Contains("-before/2-lc$^1/1-1-") || !names.Contains("-before/2-lc$^0/1-0-")
            || !names.Contains("-call_operand/2-lc$^1/1-1-") || !names.Contains("-call_operand/2-lc$^0/1-0-")
            || !names.Where(name => name.StartsWith("-crossed/2-lc$^", StringComparison.Ordinal))
                .SequenceEqual(["-crossed/2-lc$^0/1-1-", "-crossed/2-lc$^1/1-0-"]))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        var calls = new BeamCallConstructionProbe(image);
        if (calls.CallIndex(
            calls.Body("before"),
            Maps,
            "from_list",
            1
        ) < 0
            || calls.CallIndex(
                calls.Body("call_operand"),
                Maps,
                "from_keys",
                2
            ) < 0)
        {
            throw new InvalidOperationException(MissingHelper);
        }

        string withInline = Source.Replace("-export(", "-compile(inline).\n-export(", StringComparison.Ordinal);
        byte[] inlineImage = CodeGeneration.CompileBeamModule(withInline, Fixture);
        BeamModule inlineModule = BeamModule.Prepare(inlineImage, new BeamDependencySelection([Lists, Maps]));
        string[] inlineNames = inlineModule.Functions.Select(function => function.Name).ToArray();
        if (!inlineNames.Contains("-inline_bound/2-lc$^1/1-1-") || !inlineNames.Contains("-inline_bound/2-lc$^0/1-0-")
            || inlineNames.Any(name => name.StartsWith("-inline_prefix_call/2-lc$^", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        var selection = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed(Lists),
            RuntimeModuleSelection.Managed(Maps)
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term stamp = Term.A("stamp");
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term rows = Term.List(Term.Tuple(Term.A("tag"), a, one), Term.Tuple(Term.A("tag"), a, two));
            Term keys = Term.List(a, b);
            Term values = Term.List(Term.Tuple(stamp, a), Term.Tuple(stamp, b));

            Term before = await module.Invoke(
                context,
                "before",
                Term.Tuple(rows, keys),
                stamp
            );
            Term operand = await module.Invoke(
                context,
                "call_operand",
                keys,
                stamp
            );
            Term inlined = await inlineModule.Invoke(
                context,
                "inline_bound",
                Term.Tuple(rows, Term.List(a)),
                stamp
            );
            Term mapped = Term.List(Term.Tuple(a, Term.Tuple(two, two)));
            var strictIndex = new MapTerm([new(a, Term.Tuple(two, two))]);
            var keyIndex = new MapTerm([new(a, Nil.Value), new(b, Nil.Value)]);
            if (!Term.Tuple(values, strictIndex).Equals(before) || !Term.Tuple(keyIndex, values).Equals(operand)
                || !Term.Tuple(mapped, stamp).Equals(inlined))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "before",
                    Term.Tuple(Term.List(a), b),
                    stamp
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), a)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }
}
