using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMiddleDefaultSequenceChecks
{
    internal const string TestName = "compiler/beam/retains-the-required-fields-on-both-sides-of-a-default";
    private const string Fixture = "beam_middle_default_sequence_probe.erl";
    private const string IncorrectInstructions = "The middle default did not preserve adjacent fields in the selected frame.";
    private const string IncorrectResults = "The middle default changed the decoded or encoded field order.";

    private const string Source = """
        -module(beam_middle_default_sequence_probe).
        -export([encode/2, decode/2, pack/3, linked/2]).

        encode(Input, Tags) ->
            {_,A,M,C} = Input,
            {AB,AL} = encode_field(A, [<<2>>]),
            {MB,ML} = case is_default(M) of true -> {[],0}; false -> encode_field(M,[<<3>>]) end,
            {CB,CL} = encode_field(C, [<<4>>]),
            Parts = [AB,MB,CB],
            Total = AL + ML + CL,
            finish(Tags, Parts, Total).

        decode(Input, Tags) ->
            Items = read_field(Input, Tags),
            [A0|T1] = Items,
            A = read_field(A0, [2]),
            {M,T2} = case T1 of [{3,M0}|M1] -> {read_field(M0,[]),M1}; _ -> {missing,T1} end,
            [C0|T3] = T2,
            C = read_field(C0, [4]),
            case T3 of [] -> true; _ -> exit({error,{asn1,{unexpected,T3}}}) end,
            Output = {fields,A,M,C},
            Output.

        linked(Input, Tags) ->
            {_,A,B,C} = Input,
            {AB,AL} = encode_field(A, [<<2>>]),
            {BB,BL} = encode_field(B, [<<3>>]),
            {CB,CL} = encode_field(C, [<<4>>]),
            Parts = [AB,BB,CB],
            Total = AL + BL + CL,
            finish(Tags, Parts, Total).

        is_default(missing) -> true; is_default(_) -> false.
        read_field(Value, _) -> Value.
        encode_field(Value, Tags) -> pack_tags(Tags, {Value,byte_size(Value)}).
        pack_tags(Tags, {Value,Size}) -> pack(Tags,Value,Size).
        pack([], Value, Size) -> {Value,Size};
        pack([Tag|Tail], Value, Size) -> pack(Tail,Value,Size+byte_size(Tag)).
        finish(Tags, Parts, Size) -> {Tags,Parts,Size}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        object[] encode = Read("encode");
        object[] decode = Read("decode");
        object[] linked = Read("linked");
        object[] additions = linked.Where(instruction => Opcode(instruction) == GcBifTwo).ToArray();
        if (Frames(encode) is not [4]
            || !encode.Any(instruction => Opcode(instruction) == TrimFrame)
            || Frames(decode) is not [3]
            || decode.Count(instruction => Opcode(instruction) == BadMatch) != 2
            || additions is not [_, var final]
            || Items(final, "Operands").ToArray() is not [_, _, _, _, var last, _]
            || Property(last, "TypeIndex") is not null)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term a = new BitString([1]);
            Term m = new BitString([2]);
            Term c = new BitString([3]);
            Term omitted = Term.A("missing");
            Term tag = Term.A("outer");
            Term missing = Term.Tuple(
                Term.A("fields"),
                a,
                omitted,
                c
            );
            Term present = Term.Tuple(
                Term.A("fields"),
                a,
                m,
                c
            );
            if (!Term.Tuple(tag, Term.List(a, Nil.Value, c), Term.I(4)).Equals(await module.Invoke(
                context,
                "encode",
                missing,
                tag
            ))
                || !Term.Tuple(tag, Term.List(a, m, c), Term.I(6)).Equals(await module.Invoke(
                    context,
                    "encode",
                    present,
                    tag
                ))
                || !missing.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(a, c),
                    tag
                ))
                || !present.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(a, Term.Tuple(Term.I(3), m), c),
                    tag
                ))
                || !Term.Tuple(tag, Term.List(a, m, c), Term.I(6)).Equals(await module.Invoke(
                    context,
                    "linked",
                    present,
                    tag
                )))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });

        object[] Read(string name)
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

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int[] Frames(object[] body) => body.Where(instruction => Opcode(instruction) == Allocate)
        .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
