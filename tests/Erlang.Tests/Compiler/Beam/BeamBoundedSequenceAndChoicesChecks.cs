using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBoundedSequenceAndChoicesChecks
{
    internal const string TestName = "compiler/beam/keeps-a-bounded-record-remainder-and-forwarded-choice-in-live-registers";
    private const string Fixture = "beam_bounded_sequence_and_choices_probe.erl";
    private const string IncorrectInstructions = "A sequence branch did not preserve its frame or range tests.";
    private const string IncorrectResults = "A sequence branch returned unexpected values.";

    private const string Source = """
        -module(beam_bounded_sequence_and_choices_probe).
        -export([fields/2, choice/2]).

        fields(Input, Tags) ->
            Parts = prepare(Input, Tags),
            [First|Rest] = Parts,
            Count = begin
                N = sized(First, [2]),
                if 1 =< N, N =< 4 -> N;
                   true -> exit({error, {asn1, range}})
                end
            end,
            [Second|Extra] = Rest,
            Data = pass(Second, [4]),
            case Extra of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Extra}}})
            end,
            Result = {record, Count, Data},
            Result.

        choice(Input, Tags) ->
            Value = prepare(Input, Tags),
            case case Value of [One] -> One; _ -> Value end of
                {4, Bytes} -> {bytes, pass(Bytes, [])};
                {16, Values} -> {sequence, pass(Values, [])};
                {8, Payload} -> {empty, fixed(Payload, [])};
                Other -> exit({error, {asn1, {invalid_choice, Other}}})
            end.

        prepare(Input, _) -> Input.
        pass(Input, _) -> Input.
        sized(Input, _) -> byte_size(Input).
        fixed(Input, Tags) -> Value = pass(Input, Tags), case Value of value -> unit; _ -> exit({unexpected, Value}) end.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] fields = Read("fields", 3);
        object[] choice = Read("choice", 0);
        if (fields.Count(value => Opcode(value) == BadMatch) != 2
            || fields.Count(value => Opcode(value) == TailExternalCallWithFrame) != 2
            || choice.Count(value => Opcode(value) == SelectValue) != 1
            || choice.Count(value => Opcode(value) == PutTuple) != 5)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term data = new BitString([7, 8]);
            Term expectedFields = Term.Tuple(Term.A("record"), Term.I(2), data);
            Term expectedChoice = Term.Tuple(Term.A("bytes"), data);
            if (!expectedFields.Equals(await module.Invoke(
                context,
                "fields",
                Term.List(data, data),
                Nil.Value
            ))
                || !expectedChoice.Equals(await module.Invoke(
                    context,
                    "choice",
                    Term.List(Term.Tuple(Term.I(4), data)),
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });

        object[] Read(string name, int frameSize)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, _] && atoms[Value(atom)] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            if (body.Where(instruction => Opcode(instruction) == Allocate).Select(instruction =>
                Value(Items(instruction, "Operands").First())).ToArray() is not [var size] || size != frameSize)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            return body;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string member) => ((IEnumerable)Property(owner, member)).Cast<object>();
}
