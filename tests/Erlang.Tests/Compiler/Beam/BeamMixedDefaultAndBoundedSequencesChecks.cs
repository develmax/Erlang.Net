using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedDefaultAndBoundedSequencesChecks
{
    internal const string TestName = "compiler/beam/keeps-mixed-defaults-and-integer-bounds-in-compact-sequence-frames";
    private const string Fixture = "beam_mixed_default_and_bounded_sequences_probe.erl";
    private const string IncorrectInstructions = "A sequence branch did not preserve its frame or range tests.";
    private const string IncorrectResults = "A sequence branch returned unexpected values.";

    private const string Source = """
        -module(beam_mixed_default_and_bounded_sequences_probe).
        -export([encode/2, decode/2]).

        encode(Input, Tags) ->
            {_, First, Second, Optional, Default} = Input,
            {A, AN} = emit(First, []),
            {B, BN} = emit(Second, [<<2>>]),
            {C, CN} = case Optional of
                absent -> {<<>>,0};
                _ -> emit(Optional, [<<2>>])
            end,
            {D, DN} = case is_default(Default) of
                true -> {[],0};
                false -> emit(Default, [<<48>>])
            end,
            Pieces = [A,B,C,D],
            Sum = AN + BN + CN + DN,
            finish(Tags, Pieces, Sum).

        decode(Input, Tags) ->
            Parts = prepare(Input, Tags),
            [First|Rest] = Parts,
            A = pass(First, []),
            [Second|Rest2] = Rest,
            B = begin
                N = sized(Second, [2]),
                if 1 =< N, N =< upper_marker -> N;
                   true -> exit({error, {asn1, range}})
                end
            end,
            {C,Rest3} = case Rest2 of
                [{2, P}|Tail1] ->
                    {begin
                        M = sized(P, []),
                        if 1 =< M, M =< upper_marker -> M;
                           true -> exit({error, {asn1, range}})
                        end
                    end,Tail1};
                _ -> {absent, Rest2}
            end,
            {D,Extra} = case Rest3 of
                [{16, Q}|Tail2] -> {pass(Q, []),Tail2};
                _ -> {default, Rest3}
            end,
            case Extra of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Extra}}})
            end,
            Result = {record,A,B,C,D},
            Result.

        prepare(Input, _) -> Input.
        pass(Input, _) -> Input.
        sized(Input, _) -> byte_size(Input).
        emit(Input, _) -> {Input,byte_size(Input)}.
        is_default(default) -> true;
        is_default(_) -> false.
        finish(Tags, Pieces, Sum) -> {Tags,Pieces,Sum}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] encode = Read("encode", 5);
        object[] decode = Read("decode", 5);
        if (encode.Count(value => Opcode(value) == BadMatch) != 1
            || decode.Count(value => Opcode(value) == IsGreaterOrEqual) != 2
            || decode.Count(value => Opcode(value) == TailExternalCallWithFrame) != 3)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term first = new BitString([7]);
            Term second = new BitString([5, 6]);
            Term missing = Term.A("absent");
            Term omitted = Term.A("default");
            Term record = Term.A("record");
            Term input = Term.Tuple(
                record,
                first,
                second,
                missing,
                omitted
            );
            Term expectedEncode = Term.Tuple(Nil.Value, Term.List(
                first,
                second,
                new BitString([]),
                Nil.Value
            ), Term.I(3));
            Term expectedDecode = Term.Tuple(
                record,
                first,
                Term.I(2),
                missing,
                omitted
            );
            if (!expectedEncode.Equals(await module.Invoke(
                context,
                "encode",
                input,
                Nil.Value
            ))
                || !expectedDecode.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(first, second),
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
