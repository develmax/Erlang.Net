using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRequiredSequenceEncoderChecks
{
    internal const string TestName = "compiler/beam/retains-required-encoding-pairs-and-protected-list-results";
    private const string Fixture = "beam_required_sequence_encoder_probe.erl";
    private const string IncorrectFrame = "A required field or protected list helper has the wrong live frame.";

    private const string Source = """
        -module(beam_required_sequence_encoder_probe).
        -export([encode_required/2, encode_table/2, protected_map/2, foreign/2, factory/1]).

        encode_required(Input, Tags) ->
            {_, First, External, Third, Fourth} = Input,
            {A, ALen} = named(First, [{short,1}], [<<2>>]),
            {B, BLen} = ?MODULE:foreign(External, []),
            {C, CLen} = payload(Third, [<<48>>]),
            {D, DLen} = payload(Fourth, []),
            Parts = [A, B, C, D],
            Total = ALen + BLen + CLen + DLen,
            finish(Tags, Parts, Total).

        encode_table(Input, Tags) ->
            {_, Key, Field} = Input,
            Callback = ?MODULE:factory(Key),
            {A, ALen} = payload(Key, [<<6>>]),
            {B, BLen} = with_callback(Field, [<<49>>], Callback),
            Parts = [A, B],
            Total = ALen + BLen,
            finish(Tags, Parts, Total).

        protected_map(Input, Callback) ->
            Values = unpack(Input, []),
            [begin
                 Decoded = convert(V, []),
                 try Callback(type, Decoded, [])
                 catch
                     Kind:Reason when Kind =:= error; Kind =:= exit ->
                         exit({protected, Reason})
                 end
             end || V <- Values].

        named(short, [{short,1}], _) -> {<<1>>, 1};
        named(Other, _, _) -> {Other, byte_size(Other)}.
        payload(Bin, _) -> {Bin, byte_size(Bin)}.
        with_callback(Field, _, Callback) -> {Callback(Field), byte_size(Field)}.
        finish(Tag, Parts, Total) -> {Tag, Parts, Total}.
        foreign(invalid, _) -> invalid;
        foreign(Value, _) -> {Value, byte_size(Value)}.
        factory(Key) -> fun(Field) -> {Key,Field} end.
        unpack(Values, _) -> Values.
        convert(Value, _) -> byte_size(Value).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] required = Read("encode_required");
        object[] table = Read("encode_table");
        object[] protection = Read(atoms.Single(atom => atom?.StartsWith("-protected_map/2-lc$", StringComparison.Ordinal) == true));
        object[] factory = Read(atoms.Single(atom => atom?.StartsWith("-factory/1-fun-", StringComparison.Ordinal) == true));
        if (Array.IndexOf(instructions, factory[0]) > Array.IndexOf(instructions, protection[0])
            || !HasFrame(required, 4) || required.Count(instruction => Opcode(instruction) == BadMatch) != 2
            || !HasFrame(table, 3) || !table.Any(instruction => Opcode(instruction) == TrimFrame)
            || !HasFrame(protection, 4) || !protection.Any(instruction => Opcode(instruction) == TrimFrame)
            || protection.Count(instruction => Opcode(instruction) == TryCase) != 1)
        {
            throw new InvalidOperationException(IncorrectFrame);
        }

        return Task.CompletedTask;

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

    private static bool HasFrame(IEnumerable<object> body, int count) => body.Any(instruction => Opcode(instruction) == Allocate
        && Value(Items(instruction, "Operands").First()) == count);

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
