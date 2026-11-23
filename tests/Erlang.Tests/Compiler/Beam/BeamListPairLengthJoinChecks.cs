using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListPairLengthJoinChecks
{
    internal const string TestName = "compiler/beam/keeps-reverse-fold-length-types-proven-across-callers";
    private const string Fixture = "beam_list_pair_length_join_probe.erl";
    private const string IncorrectInstructions = "The counted reverse fold has the wrong frame or operand types.";
    private const string IncorrectResult = "The counted reverse fold changed an output pair.";

    private const string Source = """
        -module(beam_list_pair_length_join_probe).
        -export([known/1, joined/1, tagged/3]).

        known(Input) -> known_fold(Input, [], 0).
        known_fold([], Acc, Count) -> {lists:reverse(Acc), Count};
        known_fold([Head|Tail], Acc, Count) ->
            {Bytes, Size} = known_pair(Head, [<<48>>]),
            known_fold(Tail, [Bytes|Acc], Count + Size).

        joined(Input) -> joined_fold(Input, [], 0).
        joined_fold([], Acc, Count) -> {lists:reverse(Acc), Count};
        joined_fold([Head|Tail], Acc, Count) ->
            {Bytes, Size} = joined_pair(Head, [<<48>>]),
            joined_fold(Tail, [Bytes|Acc], Count + Size).

        known_pair(Bytes, _) -> {Bytes, byte_size(Bytes)}.
        joined_pair(Bytes, Tags) -> tagged(Tags, Bytes, calculated_size(Bytes)).
        calculated_size(<<>>) -> 0.0;
        calculated_size(Bytes) -> byte_size(Bytes);
        calculated_size(_) -> 1.5.
        tagged([], Bytes, Size) -> {Bytes, Size};
        tagged([_|Tail], Bytes, Size) -> tagged(Tail, Bytes, Size + 1).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("known_fold", true);
        Check("joined_fold", false);

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term binaries = Term.List(new BitString([1]), new BitString([2, 3]));
            Term expected = Term.Tuple(binaries, Term.I(3));
            Term taggedExpected = Term.Tuple(binaries, Term.I(5));
            if (!expected.Equals(await module.Invoke(context, "known", binaries))
                || !taggedExpected.Equals(await module.Invoke(context, "joined", binaries))
                || !Term.Tuple(Nil.Value, Term.I(0)).Equals(await module.Invoke(context, "joined", Nil.Value)))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });

        void Check(string name, bool expectNumericSecondOperand)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );

            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int[] frames = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();
            object[] adds = body.Where(instruction => Opcode(instruction) == GcBifTwo).ToArray();
            if (frames is not [3, 1]
                || adds is not [var add]
                || Items(add, "Operands").ToArray() is not [_, _, _, var first, var second, _]
                || Property(first, "TypeIndex") is null
                || (Property(second, "TypeIndex") is not null) != expectNumericSecondOperand
                || body.Any(instruction => Opcode(instruction) == BadMatch))
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
