using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNamedDefaultOnlyRecordChecks
{
    internal const string TestName = "compiler/beam/keeps-named-number-conversions-in-default-only-records";
    private const string Fixture = "beam_named_default_only_record_probe.erl";
    private const string IncorrectInstructions = "The default-only record did not use its shared call frame.";
    private const string IncorrectValue = "The default-only record lost a field, its named conversion, or its encoded length.";

    private const string Source = """
        -module(beam_named_default_only_record_probe).
        -export([encode/2, decode/2]).

        encode(Record, Tags) ->
            {_,A,B,C} = Record,
            {AB,AL} = case default(A) of true -> {[],0}; false -> named(A,[{first,1}],[<<2>>]) end,
            {BB,BL} = case default(B) of true -> {[],0}; false -> named(B,[{second,2}],[<<4>>]) end,
            {CB,CL} = case default(C) of true -> {[],0}; false -> pair(C,[<<6>>]) end,
            Bytes = [AB,BB,CB],
            Length = AL + BL + CL,
            finish(Tags,Bytes,Length).

        decode(Input, Tags) ->
            L0 = prepare(Input,Tags),
            {A,L1} = case L0 of
                [{2,H1}|R1] -> {begin N1 = read_field(H1,[]), rename(N1,[{first,1}]) end,R1};
                _ -> {0,L0}
            end,
            {B,L2} = case L1 of
                [{4,H2}|R2] -> {begin N2 = read_field(H2,[]), rename(N2,[{second,2}]) end,R2};
                _ -> {0,L1}
            end,
            {C,L3} = case L2 of [{6,H3}|R3] -> {read_field(H3,[]),R3}; _ -> {0,L2} end,
            case L3 of [] -> true; _ -> exit({error,{asn1,{unexpected,L3}}}) end,
            Record = {decoded,A,B,C},
            Record.

        default(0) -> true; default(_) -> false.
        named(Value, [{Name,_}], _) -> {{Name,Value},1}.
        pair(Value, _) -> {Value,1}.
        finish(Tags, Bytes, Size) -> {Tags,Bytes,Size}.
        prepare(Value, _) -> Value.
        read_field(Value, _) -> Value.
        rename(Number, [{Name,Number}]) -> Name;
        rename(Number, _) -> Number.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        CheckFrame("encode");
        CheckFrame("decode");

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term zero = Term.I(0);
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term decoded = Term.A("decoded");
            Term tags = Term.A("outer");
            Term expected = Term.Tuple(tags, Term.List(Term.Tuple(first, one), Nil.Value, two), two);
            Term encoded = await module.Invoke(
                context,
                "encode",
                Term.Tuple(
                    decoded,
                    one,
                    zero,
                    two
                ),
                tags
            );
            Term allAbsent = await module.Invoke(
                context,
                "decode",
                Nil.Value,
                tags
            );
            Term middlePresent = await module.Invoke(
                context,
                "decode",
                Term.List(Term.Tuple(Term.I(4), two)),
                tags
            );
            Term firstPresent = await module.Invoke(
                context,
                "decode",
                Term.List(Term.Tuple(two, one)),
                tags
            );
            if (!expected.Equals(encoded)
                || !Term.Tuple(
                    decoded,
                    zero,
                    zero,
                    zero
                ).Equals(allAbsent)
                || !Term.Tuple(
                    decoded,
                    zero,
                    second,
                    zero
                ).Equals(middlePresent)
                || !Term.Tuple(
                    decoded,
                    first,
                    zero,
                    zero
                ).Equals(firstPresent))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        void CheckFrame(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );
            if (start < 0 || instructions.Skip(start + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray() is not [4])
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
