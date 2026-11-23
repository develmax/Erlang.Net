using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDecodedIntegerPrefixChecks
{
    internal const string TestName = "compiler/beam/joins-decoded-integer-prefixes-in-registers";
    private const string Fixture = "beam_decoded_integer_prefix_probe.erl";
    private const string WrongSchedule = "The integer prefix did not stay in X registers through the join.";
    private const string WrongResult = "The optimized integer prefix changed an offset or rejection.";
    private const string Source = """
        -module(beam_decoded_integer_prefix_probe).
        -export([decode/2, shifted/2]).
        decode(B, Type) ->
            Bytes = extract(B, Type),
            [First | Rest] = decode_values(Bytes),
            {Tag, Value} = if
                First < 40 ->
                    {0, First};
                First < 80 ->
                    {1, First - 40};
                true ->
                    {2, First - 80}
            end,
            list_to_tuple([Tag, Value | Rest]).
        shifted(B, Type) ->
            Bytes = extract(B, Type),
            [First | Rest] = decode_values(Bytes),
            {Tag, Value} = if
                First < 40 ->
                    {0, First};
                First < 80 ->
                    {1, First - 39};
                true ->
                    {2, First - 80}
            end,
            list_to_tuple([Tag, Value | Rest]).
        extract(B, direct) ->
            B;
        extract(_B, default) ->
            <<43>>.
        decode_values(<<Bytes/binary>>) ->
            values(Bytes, 0, 0).
        values(<<1:1, H:7,T/binary>>, N, Acc) when N < 5 ->
            V = (Acc bsl 7) bor H,
            values(T, N + 1, V);
        values(<<H,T/binary>>, N, Acc) when N < 5 ->
            V = (Acc bsl 7) bor H,
            [V | values(T, 0, 0)];
        values(<<>>, _N, _Acc) ->
            [].
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] direct = Function("decode");
        object[] shifted = Function("shifted");
        object[] frames = direct.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (frames is not [var frame] || Items(frame, "Operands").ToArray() is not [var slots, var live]
            || Value(slots) != 0 || Value(live) != 2
            || direct.Count(instruction => Opcode(instruction) == LocalCall) != 2
            || direct.Count(instruction => Opcode(instruction) == IsGreaterOrEqual) != 2
            || direct.Count(instruction => Opcode(instruction) == PutList) != 2
            || direct.Any(instruction => Opcode(instruction) == PutTuple)
            || !direct.Any(instruction => Opcode(instruction) == TailExternalCallWithFrame)
            || !shifted.Any(instruction => Opcode(instruction) == PutTuple))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            var cases = new (string Function, byte[] Bytes, Term Expected)[]
            {
                ("decode", [39, 4], Term.Tuple(Term.I(0), Term.I(39), Term.I(4))),
                ("decode", [40], Term.Tuple(Term.I(1), Term.I(0))),
                ("decode", [79], Term.Tuple(Term.I(1), Term.I(39))),
                ("decode", [80], Term.Tuple(Term.I(2), Term.I(0))),
                ("decode", [129, 1, 3], Term.Tuple(Term.I(2), Term.I(49), Term.I(3))),
                ("shifted", [40], Term.Tuple(Term.I(1), Term.I(1)))
            };
            foreach (var (function, bytes, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    new BitString(bytes),
                    Term.A("direct")
                )))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "decode",
                    new BitString([]),
                    Term.A("direct")
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Nil.Value)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });

        object[] Function(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var count]
                    && atoms[Value(atom)] == name && Value(count) == 2
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
