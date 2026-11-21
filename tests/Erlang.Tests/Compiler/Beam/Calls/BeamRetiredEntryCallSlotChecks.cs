using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetiredEntryCallSlotChecks
{
    internal const string TestName = "compiler/beam/reuses-retired-entry-slot-before-following-call";
    private const string WrongLayout = "Expected outgoing operands to retire before the result uses their stack slot.";
    private const string WrongValue = "Unexpected result or restoration across a retired entry stack slot.";
    private const string Source = """
        -module(retired_entry_call_slot).
        -export([returning/3, checked/3, probing/1, restore_value/0, probe_map/0]).

        returning({Key}, Map, Seed) ->
            returning(Key, Map, Seed);
        returning(Key, Map, Seed) when is_atom(Key) ->
            Previous = retain(Seed),
            Result = maps:get(Key, Map),
            _ = restore(Previous),
            Result.

        checked({Key}, Map, Seed) ->
            checked(Key, Map, Seed);
        checked(Key, Map, Seed) when is_atom(Key) ->
            Previous = retain(Seed),
            true = maps:get(Key, Map),
            _ = restore(Previous),
            ok.

        probing({Map}) ->
            probing(Map);
        probing(Map) when is_map(Map) ->
            Previous = retain(probe),
            _ = maps:get(node(), Map),
            Previous.

        retain(Value) ->
            _ = erlang:put(retired_input, Value),
            Value.

        restore(Value) ->
            erlang:put(retired_restore, Value).

        restore_value() ->
            erlang:get(retired_restore).

        probe_map() ->
            #{node() => found}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "retired_entry_call_slot.erl");
        object[] operations = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, operations.Length)
            .Where(index => Opcode(operations[index]) == FunctionInfo).ToArray();
        if (entries.Length < 4)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int[] widths = [2, 2, 1];
        for (int index = 0; index < widths.Length; index++)
        {
            object[] body = operations[entries[index]..entries[index + 1]];
            var allocations = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Operand(instruction, 0)).ToArray();
            if (allocations.Length != 1 || allocations[0] != widths[index]
                || index == 0 && !body.Any(instruction => Opcode(instruction) == Swap)
                || index == 1 && !body.Any(instruction => Opcode(instruction) == BadMatch))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term falsy = new MapTerm([new(key, Term.A("false"))]);
            Term truthy = new MapTerm([new(key, Term.A("true"))]);
            try
            {
                await image.Invoke(
                    context,
                    "checked",
                    key,
                    falsy,
                    Term.A("should_not_restore")
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Term.A("false"))))
            {
            }

            if (!Term.A("undefined").Equals(await image.Invoke(context, "restore_value"))
                || !Term.A("false").Equals(
                    await image.Invoke(
                        context,
                        "returning",
                        key,
                        falsy,
                        Term.A("retained")
                    ))
                || !Term.A("retained").Equals(await image.Invoke(context, "restore_value"))
                || !Term.A("ok").Equals(
                    await image.Invoke(
                        context,
                        "checked",
                        key,
                        truthy,
                        Term.A("checked_value")
                    ))
                || !Term.A("checked_value").Equals(await image.Invoke(context, "restore_value")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term map = await image.Invoke(context, "probe_map");
            if (!Term.A("probe").Equals(await image.Invoke(context, "probing", map)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
