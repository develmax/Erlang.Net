using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingleSizeCheckedRecordChecks
{
    internal const string TestName = "compiler/beam/keeps-only-the-required-fields-live-with-a-single-size-check";
    private const string Fixture = "beam_single_size_checked_record_probe.erl";
    private const string IncorrectLayout = "A decoder with one size constraint emitted the wrong frame or failure blocks.";
    private const string IncorrectResult = "A decoder with one size constraint returned an unexpected record.";

    private const string Source = """
        -module(beam_single_size_checked_record_probe).
        -export([first/2, last/2]).

        first(Input, Tags) ->
            Fields = prepare(Input, Tags),
            [Head|Tail] = Fields,
            A = begin
                Bytes = decode(Head, [1]),
                N = byte_size(Bytes),
                if N =:= 2 -> Bytes;
                   true -> exit({error, {asn1, bad_range}})
                end
            end,
            [Value|Rest] = Tail,
            B = decode(Value, [2]),
            case Rest of [] -> true; _ -> exit({error, {asn1, {unexpected, Rest}}}) end,
            Record = {decoded, A, B},
            Record.

        last(Input, Tags) ->
            Fields = prepare(Input, Tags),
            [Head|Tail] = Fields,
            A = decode(Head, [1]),
            [Value|Rest] = Tail,
            B = begin
                Bytes = decode(Value, [2]),
                N = byte_size(Bytes),
                if N =:= 2 -> Bytes;
                   true -> exit({error, {asn1, bad_range}})
                end
            end,
            case Rest of [] -> true; _ -> exit({error, {asn1, {unexpected, Rest}}}) end,
            Record = {decoded, A, B},
            Record.

        prepare(Input, _) -> Input.
        decode(Value, _) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        foreach (string name in new[] { "first", "last" })
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );

            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectLayout);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int[] frames = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();

            if (frames is not [3]
                || body.Count(instruction => Opcode(instruction) == GcBifOne) != 1
                || body.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != 2
                || body.Count(instruction => Opcode(instruction) == BadMatch) != 2)
            {
                throw new InvalidOperationException(IncorrectLayout);
            }
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term binary = new BitString([1, 2]);
            Term marker = Term.A("untested");
            Term first = await module.Invoke(
                context,
                "first",
                Term.List(binary, marker),
                Nil.Value
            );
            Term last = await module.Invoke(
                context,
                "last",
                Term.List(marker, binary),
                Nil.Value
            );
            if (!Term.Tuple(Term.A("decoded"), binary, marker).Equals(first)
                || !Term.Tuple(Term.A("decoded"), marker, binary).Equals(last))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
