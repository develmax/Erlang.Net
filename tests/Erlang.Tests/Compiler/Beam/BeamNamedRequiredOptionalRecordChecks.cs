using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNamedRequiredOptionalRecordChecks
{
    internal const string TestName = "compiler/beam/keeps-named-and-bounded-record-fields-live-until-optional-encoding";
    private const string Fixture = "beam_named_required_optional_record_probe.erl";
    private const string IncorrectInstructions = "The named or bounded field sequence emitted the wrong branch frames.";
    private const string IncorrectResults = "The named or bounded field sequence returned an unexpected value.";

    private const string Source = """
        -module(beam_named_required_optional_record_probe).
        -export([encode/2, small/2, decode/2]).

        encode(Input, Tags) ->
            {_,A,B,C,D} = Input,
            {AB,AL} = named(A,[{named,1}],[<<1>>]),
            {BB,BL} = emit_field(B,[<<2>>]),
            {CB,CL} = emit_field(C,[<<3>>]),
            {DB,DL} = case D of absent -> {<<>>,0}; _ -> emit_field(D,[<<4>>]) end,
            Parts = [AB,BB,CB,DB],
            Size = AL + BL + CL + DL,
            finish(Tags,Parts,Size).

        small(Input, Tags) ->
            {_,A,B} = Input,
            {AB,AL} = case A of absent -> {<<>>,0}; _ -> emit_field(A,[<<1>>]) end,
            {BB,BL} = emit_field(B,[<<2>>]),
            Parts = [AB,BB],
            Size = AL + BL,
            finish(Tags,Parts,Size).

        decode(Input, Tags) ->
            Items = passthrough(Input,Tags),
            [A0|L1] = Items,
            A = begin V = integer_field(A0,[1]), rename(V,[{named,1}]) end,
            [B0|L2] = L1,
            B = begin N = integer_field(B0,[2]),
                if 1 =< N, N =< 4 -> N; true -> exit({error,{asn1,bad_range}}) end
            end,
            [C0|L3] = L2,
            C = begin K = integer_field(C0,[3]),
                if 2 =< K, K =< 8 -> K; true -> exit({error,{asn1,bad_range}}) end
            end,
            {D,L4} = case L3 of [{4,D0}|T] -> {passthrough(D0,[]),T}; _ -> {absent,L3} end,
            case L4 of [] -> true; _ -> exit({error,{asn1,{unexpected,L4}}}) end,
            Output = {record,A,B,C,D},
            Output.

        named(Value, _, _) -> {Value,2}.
        emit_field(Value, _) -> {Value,1}.
        finish(Tags, Values, Size) -> {Tags,Values,Size}.
        passthrough(Value, _) -> Value.
        integer_field(Value, _) -> byte_size(Value).
        rename(1, _) -> named; rename(Value, _) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] encode = Read("encode");
        object[] small = Read("small");
        object[] decode = Read("decode");
        if (Frames(encode) is not [4]
            || Frames(small) is not [3]
            || !small.Any(instruction => Opcode(instruction) == TrimFrame)
            || Frames(decode) is not [5]
            || decode.Count(instruction => Opcode(instruction) == BadMatch) != 3
            || decode.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != 3)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term absent = Term.A("absent");
            Term single = new BitString([1]);
            Term doublet = new BitString([2, 3]);
            Term trio = new BitString([1, 2, 3]);
            Term tags = Term.A("tags");
            Term encoded = Term.Tuple(tags, Term.List(
                single,
                doublet,
                trio,
                new BitString([])
            ), Term.I(4));
            Term smallEncoded = Term.Tuple(tags, Term.List(new BitString([]), single), Term.I(1));
            Term decoded = Term.Tuple(
                Term.A("record"),
                Term.A("named"),
                Term.I(2),
                Term.I(3),
                absent
            );
            if (!encoded.Equals(await module.Invoke(
                context,
                "encode",
                Term.Tuple(
                    Term.A("record"),
                    single,
                    doublet,
                    trio,
                    absent
                ),
                tags
            ))
                || !smallEncoded.Equals(await module.Invoke(
                    context,
                    "small",
                    Term.Tuple(Term.A("record"), absent, single),
                    tags
                ))
                || !decoded.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(single, doublet, trio),
                    tags
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
