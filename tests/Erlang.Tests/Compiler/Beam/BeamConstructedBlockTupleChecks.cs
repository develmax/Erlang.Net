using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstructedBlockTupleChecks
{
    internal const string TestName = "compiler/beam/forwards-known-block-tuple-bindings";
    private const string WrongSchedule = "The known tuple was constructed and matched instead of forwarding its bindings.";
    private const string WrongResult = "The block tuple match changed evaluation order, a value, or a failure.";
    private const string Fixture = "beam_constructed_block_tuple.erl";
    private const string Source = """
        -module(beam_constructed_block_tuple).
        -export([ordered/1, checked/2, prefix_bound/2, marker/0]).

        ordered(Map) ->
            {_, Chosen} = begin
                erlang:put(block_tuple_order, first),
                #{left := A, right := B} = Map,
                erlang:put(block_tuple_order, second),
                {A, B}
            end,
            Chosen.

        checked(Map, Expected) ->
            {Expected, Chosen} = begin
                #{left := A, right := B} = Map,
                {A, B}
            end,
            Chosen.

        prefix_bound(Input, Other) ->
            {Bound, Chosen} = begin
                Bound = identity(Input),
                Next = identity(Other),
                {Next, Bound}
            end,
            Chosen.

        marker() -> erlang:get(block_tuple_order).

        identity(Value) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[(int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")] == "ordered"
        );
        if (start < 0 || instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) is IsTuple or TestArity or PutTuple))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term mismatch = Term.A("mismatch");
            Term both = new MapTerm([new(left, first), new(right, second)]);
            Term partial = new MapTerm([new(left, first)]);
            if (!second.Equals(await module.Invoke(context, "ordered", both))
                || !second.Equals(await module.Invoke(context, "marker"))
                || !second.Equals(await module.Invoke(
                    context,
                    "checked",
                    both,
                    first
                ))
                || !first.Equals(await module.Invoke(
                    context,
                    "prefix_bound",
                    first,
                    first
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await ExpectBadMatch("ordered", partial, [partial]);
            if (!first.Equals(await module.Invoke(context, "marker")))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await ExpectBadMatch("checked", Term.Tuple(first, second), [both, mismatch]);
            await ExpectBadMatch("prefix_bound", Term.Tuple(second, first), [first, second]);

            return Nil.Value;

            async Task ExpectBadMatch(string name, Term unmatched, Term[] arguments)
            {
                try
                {
                    _ = await module.Invoke(context, name, arguments);
                }
                catch (ErlangException error) when (Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), unmatched).Equals(error.Reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
