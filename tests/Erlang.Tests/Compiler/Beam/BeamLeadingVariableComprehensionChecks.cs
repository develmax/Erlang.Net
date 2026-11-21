using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLeadingVariableComprehensionChecks
{
    internal const string TestName = "compiler/beam/leading-variable-comprehension";
    private const string Module = "beam_leading_variable_mixed_defs";
    private const string Source = """
        -module(beam_leading_variable_mixed_defs).
        -export([fill/2, leading_effect/2]).
        -record(state, {items=[]}).

        fill(Current, #state{items=[]}) ->
            Current;
        fill({Tag, Values, Extra}, #state{items=Items}) ->
            F = fun({Name, TagValue}, Acc) when TagValue =:= Tag -> [{Name, TagValue}|Acc];
                   (_, Acc) -> Acc
                end,
            Folded = lists:foldl(F, Values, Items),
            Names = [Name || {Name, Value} <- Items, Value =:= Tag],
            N = fun({Name, Value}) -> {Name, Value, lists:member(Name, Names)} end,
            {Tag, lists:map(N, Folded), Extra}.

        leading_effect(Current, #state{items=[]}) ->
            [tagged(Item) || Item <- Current];
        leading_effect({Tag, Values, Extra}, #state{items=Items}) ->
            F = fun({Name, TagValue}, Acc) when TagValue =:= Tag -> [{Name, TagValue}|Acc];
                   (_, Acc) -> Acc
                end,
            Folded = lists:foldl(F, Values, Items),
            Names = [Name || {Name, Value} <- Items, Value =:= Tag],
            N = fun({Name, Value}) -> {Name, Value, lists:member(Name, Names)} end,
            {Tag, lists:map(N, Folded), Extra}.

        tagged(Item) -> {early, Item}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .ToArray();
        var localDefinitions = functions.Where(function => function.Name.StartsWith("-fill/2-", StringComparison.Ordinal)
            && (function.Name.Contains("-fun-", StringComparison.Ordinal)
                || function.Name.Contains("-lc$^", StringComparison.Ordinal)));
        if (!localDefinitions.SequenceEqual([
                ("-fill/2-fun-2-", 2),
                ("-fill/2-lc$^1/1-0-", 2),
                ("-fill/2-fun-0-", 3)
            ])
            || functions.Any(function => function.Name.StartsWith("-leading_effect/2-lc$^", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("The leading variable arm must preserve the later helper and its closure ordering.");
        }

        await using var runtime = new ProcessRuntime();
        var dependencies = new BeamDependencySelection(["lists", ErlangModuleNames.Module]);
        BeamModule image = BeamModule.Prepare(beam, dependencies);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term state = Term.A("state");
            Term selected = Term.A("selected");
            Term saved = Term.A("saved");
            Term old = Term.A("old");
            Term hit = Term.A("hit");
            Term extra = Term.A("extra");
            Term start = Term.Tuple(selected, Term.List(Term.Tuple(old, saved)), extra);
            Term values = Term.Tuple(state, Term.List(Term.Tuple(hit, selected), Term.Tuple(Term.A("miss"), Term.A("different"))));
            Term expected = Term.Tuple(selected, Term.List(
                Term.Tuple(hit, selected, Term.A("true")),
                Term.Tuple(old, saved, Term.A("false"))
            ), extra);
            Term shortcut = Term.A("shortcut");
            Term actual = await image.Invoke(
                context,
                "fill",
                start,
                values
            );
            Term empty = await image.Invoke(
                context,
                "fill",
                shortcut,
                Term.Tuple(state, Nil.Value)
            );
            if (!expected.Equals(actual) || !shortcut.Equals(empty))
            {
                throw new InvalidOperationException("The generated fold, filter and map must preserve both source branches.");
            }

            return Nil.Value;
        });
    }
}
