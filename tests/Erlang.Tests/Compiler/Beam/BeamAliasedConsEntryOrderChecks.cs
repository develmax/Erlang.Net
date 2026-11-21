using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAliasedConsEntryOrderChecks
{
    internal const string TestName = "compiler/beam/checks-aliased-cons-before-empty-entry";
    private const string WrongLayout = "Expected the disjoint aliased cons arm to own its entry check.";
    private const string WrongValue = "Unexpected value from an aliased cons entry or its nil fallback.";
    private const string Source = """
        -module(aliased_cons_entry_order).
        -export([owned/2, reversed/2, repeated/2]).

        owned(Map, []) ->
            {empty, Map};
        owned(Map, Whole = [Head | Tail]) ->
            Keys = maps:keys(Map),
            {Keys, Head, Tail, Whole}.

        reversed([], Map) ->
            {empty, Map};
        reversed([Head | Tail] = Whole, Map) ->
            Keys = maps:keys(Map),
            {Keys, Head, Tail, Whole}.

        repeated(Map, []) ->
            {empty, Map};
        repeated(Map, Whole = [Head | Head]) ->
            Keys = maps:keys(Map),
            {Keys, Head, Whole}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "aliased_cons_entry_order.erl");
        byte[] instructions = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => instructions[index] == FunctionInfo).ToArray();

        if (entries.Length < 3)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        for (int index = 0; index < 2; index++)
        {
            byte[] body = instructions[entries[index]..entries[index + 1]]
                .Where(opcode => opcode is not (Label or Line or FunctionInfo)).ToArray();
            if (body is not [IsNonemptyList, ..] || !body.Contains(IsNil))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term map = new MapTerm([]);
            Term cons = Term.List(Term.I(7), Term.I(8));
            Term expected = Term.Tuple(
                Nil.Value,
                Term.I(7),
                Term.List(Term.I(8)),
                cons
            );
            Term empty = Term.Tuple(Term.A("empty"), map);
            if (!expected.Equals(await image.Invoke(
                context,
                "owned",
                map,
                cons
            ))
                || !expected.Equals(await image.Invoke(
                    context,
                    "reversed",
                    cons,
                    map
                ))
                || !empty.Equals(await image.Invoke(
                    context,
                    "owned",
                    map,
                    Nil.Value
                ))
                || !empty.Equals(await image.Invoke(
                    context,
                    "reversed",
                    Nil.Value,
                    map
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await image.Invoke(
                    context,
                    "repeated",
                    map,
                    cons
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
            }

            return Nil.Value;
        });
    }
}
