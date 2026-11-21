using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRequiredTableSequenceDecoderChecks
{
    internal const string TestName = "compiler/beam/keeps-decoded-fields-and-remainders-in-their-live-registers";
    private const string Fixture = "beam_required_table_sequence_decoder_probe.erl";
    private const string IncorrectFrame = "A required sequence spilled a completed field or lost a malformed remainder.";
    private const string IncorrectResult = "A required sequence returned the wrong fields.";

    private const string Source = """
        -module(beam_required_table_sequence_decoder_probe).
        -export([decode/2, named/2]).

        decode(Input, Tags) ->
            Parts = unpack(Input, Tags),
            [RawKey|Rest] = Parts,
            Key = key(RawKey, []),
            Callback = lists:reverse(Key),
            [RawValue|Extra] = Rest,
            Value = field(RawValue, [17], Callback),
            case Extra of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Extra}}})
            end,
            Result = {attribute, Key, Value},
            Result.

        named(Input, Tags) ->
            Parts = unpack(Input, Tags),
            [RawKey|Rest] = Parts,
            Key = begin
                Value = key(RawKey, []),
                rename(Value, [{one,1}])
            end,
            [RawSecond|Rest2] = Rest,
            Second = lists:reverse(RawSecond, []),
            [RawThird|Rest3] = Rest2,
            Third = key(RawThird, [16]),
            [RawLast|Extra] = Rest3,
            Last = key(RawLast, []),
            case Extra of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Extra}}})
            end,
            Result = {record, Key, Second, Third, Last},
            Result.

        unpack(Value, _) -> Value.
        key(Value, _) -> Value.
        rename(1, [{one,1}]) -> one;
        rename(Value, _) -> Value.
        field(Raw, _, Callback) -> {Callback,Raw}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("decode", 3, 2);
        Check("named", 5, 4);

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term key = Term.I(1);
            Term item = Term.I(2);
            Term inputKey = Term.List(key, item);
            Term reversed = Term.List(item, key);
            Term attribute = Term.Tuple(Term.A("attribute"), inputKey, Term.Tuple(reversed, item));
            Term record = Term.Tuple(
                Term.A("record"),
                Term.A("one"),
                reversed,
                Term.I(3),
                Term.I(4)
            );
            if (!attribute.Equals(await module.Invoke(
                context,
                "decode",
                Term.List(inputKey, item),
                Nil.Value
            ))
                || !record.Equals(await module.Invoke(
                    context,
                    "named",
                    Term.List(
                        key,
                        inputKey,
                        Term.I(3),
                        Term.I(4)
                    ),
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });

        void Check(string function, int frameSize, int splits)
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

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            if (body.Count(instruction => Opcode(instruction) == BadMatch) != splits
                || body.Count(instruction => Opcode(instruction) == InitYRegisters) != splits
                || body.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != 1
                || !body.Any(instruction => Opcode(instruction) == Allocate
                    && Value(Items(instruction, "Operands").First()) == frameSize))
            {
                throw new InvalidOperationException(IncorrectFrame);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
