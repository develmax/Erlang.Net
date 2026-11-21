using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCountedListPairAccumulatorChecks
{
    internal const string TestName = "compiler/beam/keeps-only-live-values-in-a-counted-pair-fold-frame";
    private const string Fixture = "beam_counted_list_pair_accumulator_probe.erl";
    private const string IncorrectFrame = "A counted list fold did not preserve its live argument layout.";
    private const string IncorrectResult = "A counted list fold changed an output pair.";

    private const string Source = """
        -module(beam_counted_list_pair_accumulator_probe).
        -export([direct/1, with_callback/2, unchecked/1]).

        direct(Input) -> direct_fold(Input, [], 0).
        direct_fold([], Acc, Count) -> {ordered(Acc), Count};
        direct_fold([Head|Tail], Acc, Count) ->
            {Bytes, Size} = encode(Head, []),
            direct_fold(Tail, [Bytes|Acc], Count + Size).

        with_callback(Input, Callback) -> callback_fold(Input, Callback, [], 0).
        callback_fold([], _, Acc, Count) -> {ordered(Acc), Count};
        callback_fold([Head|Tail], Callback, Acc, Count) ->
            {Payload, _} = Callback(field, Head, []),
            {Bytes, Size} = encode(Payload, []),
            callback_fold(Tail, Callback, [Bytes|Acc], Count + Size).

        unchecked(Input) -> unchecked_fold(Input, [], 0).
        unchecked_fold([], Acc, Count) -> {ordered(Acc), Count};
        unchecked_fold([Head|Tail], Acc, Count) ->
            {Bytes, Size} = unknown(Head, []),
            unchecked_fold(Tail, [Bytes|Acc], Count + Size).

        unknown(Value, _) -> Value.
        encode(Bytes, _) -> {Bytes, byte_size(Bytes)}.
        ordered(Items) -> lists:reverse(Items).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        Check("direct_fold", 3, expectsCallback: false);
        Check("callback_fold", 4, expectsCallback: true);

        if (!Read("unchecked_fold").Any(instruction => Opcode(instruction) == BadMatch))
        {
            throw new InvalidOperationException(IncorrectFrame);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term binaries = Term.List(new BitString([1]), new BitString([2, 3]));
            Term pairs = Term.List(Term.Tuple(Term.I(1), Term.I(3)), Term.Tuple(Term.I(2), Term.I(4)));
            if (!Term.Tuple(binaries, Term.I(3)).Equals(await module.Invoke(context, "direct", binaries))
                || !Term.Tuple(Nil.Value, Term.I(0)).Equals(await module.Invoke(context, "direct", Nil.Value))
                || !Term.Tuple(Term.List(Term.I(1), Term.I(2)), Term.I(7)).Equals(await module.Invoke(context, "unchecked", pairs)))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });

        void Check(string function, int arity, bool expectsCallback)
        {
            object[] body = Read(function);
            int[] frames = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();
            if (frames is not [var loop, 1] || loop != arity
                || body.Any(instruction => Opcode(instruction) == CallFun) != expectsCallback
                || body.Any(instruction => Opcode(instruction) == BadMatch) != expectsCallback)
            {
                throw new InvalidOperationException(IncorrectFrame);
            }
        }

        object[] Read(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, _] && atoms[Value(atom)] == function
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectFrame);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
