using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamIntegerPairEntryChecks
{
    internal const string TestName = "compiler/beam/retains-integer-pair-fields-at-recursive-counter-entries";
    private const string Fixture = "beam_integer_pair_entry_probe.erl";
    private const string WrongType = "Only a closed integer counter should retain its integer entry operand.";
    private const string WrongValue = "The integer recursion proof changed a decode value or an unproven input failure.";
    private const string Source = """
        -module(beam_integer_pair_entry_probe).
        -export([decode/1, mixed/1, open_binary/1, open/2]).

        decode(Bytes) ->
            {N, Rest} = case Bytes of
                <<0:1,Count:7,Tail/bitstring>> -> {Count,Tail};
                <<1:1,Count:7,Tail/bitstring>> ->
                    Doubled = Count*2, {Doubled,Tail}
            end,
            collect(N, Rest, []).

        mixed(Bytes) ->
            {N, Rest} = case Bytes of
                <<0:1,Count:7,Tail/bitstring>> -> {Count,Tail};
                <<1:1,Count:7,Tail/bitstring>> -> {Count/1,Tail}
            end,
            mixed_collect(N, Rest, []).

        open_binary(Bytes) ->
            {N, Rest} = case Bytes of
                <<Count:8,Tail/bitstring>> -> {Count,Tail}
            end,
            open_collect(N, Rest, []).
        open(N, Bytes) -> open_collect(N, Bytes, []).

        collect(0, Bytes, Acc) -> {Acc,Bytes};
        collect(N, Bytes, Acc) ->
            {Value,Rest} = item(Bytes), collect(N-1, Rest, [Value|Acc]).
        mixed_collect(0, Bytes, Acc) -> {Acc,Bytes};
        mixed_collect(N, Bytes, Acc) ->
            {Value,Rest} = item(Bytes), mixed_collect(N-1, Rest, [Value|Acc]).
        open_collect(0, Bytes, Acc) -> {Acc,Bytes};
        open_collect(N, Bytes, Acc) ->
            {Value,Rest} = item(Bytes), open_collect(N-1, Rest, [Value|Acc]).

        item(Bytes) ->
            <<Value:8,Rest/bitstring>> = Bytes,
            {Value,Rest}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!TypedEntryZero("collect") || TypedEntryZero("mixed_collect") || TypedEntryZero("open_collect"))
        {
            throw new InvalidOperationException(WrongType);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term suffix = new BitString([99]);
            Term empty = new BitString([]);
            if (!Term.Tuple(Nil.Value, suffix).Equals(await module.Invoke(context, "decode", new BitString([0, 99])))
                || !Term.Tuple(Term.List(Term.I(10)), suffix).Equals(
                    await module.Invoke(context, "decode", new BitString([1, 10, 99]))
                )
                || !Term.Tuple(Term.List(Term.I(20), Term.I(10)), suffix).Equals(
                    await module.Invoke(context, "decode", new BitString([129, 10, 20, 99]))
                )
                || !Term.Tuple(Nil.Value, empty).Equals(await module.Invoke(context, "open_binary", new BitString([0]))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await Reject("mixed", [new BitString([128])]);
            await Reject("open", [new FloatTerm(0.0), empty]);

            return Nil.Value;

            async Task Reject(string function, Term[] arguments)
            {
                try
                {
                    await module.Invoke(context, function, arguments);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), empty)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });

        bool TypedEntryZero(string function)
        {
            int start = Array.FindIndex(
                instructions,
                operation => Opcode(operation) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(operation, "Operands").ElementAt(1), "Value")] == function
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongType);
            }

            object comparison = instructions.Skip(start + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .First(instruction => Opcode(instruction) == ExactEqualTest);
            object selector = Items(comparison, "Operands").ElementAt(1);

            return selector.GetType().GetProperty("TypeIndex")!.GetValue(selector) is int;
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
