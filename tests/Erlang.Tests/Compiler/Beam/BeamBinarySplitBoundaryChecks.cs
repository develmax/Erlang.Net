using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBinarySplitBoundaryChecks
{
    internal const string TestName = "compiler/beam/retains-original-binary-at-proven-split";
    private const string Fixture = "beam_binary_split_boundary.erl";
    private const string WrongSchedule = "The binary split did not retain its expected call ownership.";
    private const string WrongResult = "The binary split changed its result or failure reason.";

    private const string Source = """
        -module(beam_binary_split_boundary).
        -export([probe/1,single/1]).
        probe(Bits) ->
            case envelope(Bits) of {left,middle,Payload,right} -> parts(Payload) end.
        single(Bits) -> one(Bits).
        envelope(<<_:8,Payload/binary>>) -> {left,middle,Payload,right}.
        one(Bits) ->
            {rest,AfterTag} = tag(Bits),
            {rest,AfterValue} = consume(AfterTag),
            Size = byte_size(Bits) - byte_size(AfterValue),
            <<Prefix:Size/binary,Tail/binary>> = Bits,
            {Prefix,Tail}.
        parts(<<>>) -> [];
        parts(Bits) ->
            {rest,AfterTag} = tag(Bits),
            {rest,AfterValue} = consume(AfterTag),
            Size = byte_size(Bits) - byte_size(AfterValue),
            <<Prefix:Size/binary,Tail/binary>> = Bits,
            [Prefix | parts(Tail)].
        consume(Input) ->
            case measure(Input) of
                {forever,Payload} -> forward(Payload);
                {Size,Payload} ->
                    <<_:Size/unit:8,Rest/binary>> = Payload,
                    {rest,Rest}
            end.
        forward(<<0,0,Rest/binary>>) -> {rest,Rest};
        forward(Payload) ->
            {rest,AfterTag} = tag(Payload),
            {rest,AfterValue} = consume(AfterTag),
            forward(AfterValue).
        tag(<<_:3,31:5,Rest/binary>>) -> tag_bytes(Rest);
        tag(<<_:3,Kind:5,Rest/binary>>) -> {rest,Rest}.
        tag_bytes(<<1:1,_:7,Rest/binary>>) -> tag_bytes(Rest);
        tag_bytes(<<0:1,_:7,Rest/binary>>) -> {rest,Rest}.
        strip(<<_:8,Rest/binary>>) -> {left,Rest}.
        measure(<<1:1,0:7,Tail/binary>>) -> {forever,Tail};
        measure(<<0:1,Count:7,Tail/binary>>) -> {Count,Tail};
        measure(<<1:1,Width:7,Count:Width/unit:8,Tail/binary>>) -> {Count,Tail}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unproved = CodeGeneration.CompileBeamModule(
            Source.Replace("{left,middle,Payload,right}.", "{left,middle,unknown,right}.", StringComparison.Ordinal),
            Fixture
        );
        byte[] exported = CodeGeneration.CompileBeamModule(
            Source.Replace("[probe/1,single/1]", "[probe/1,single/1,parts/1]", StringComparison.Ordinal),
            Fixture
        );

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = new BitString([4, 1, 7]);
            Term second = new BitString([31, 129, 1, 1, 9]);
            Term nested = new BitString([48, 128, 4, 1, 7, 0, 0]);
            var multiple = new BitString([0xff, 4, 1, 7, 31, 129, 1, 1, 9, 48, 128, 4, 1, 7, 0, 0]);
            if (!Term.List(first, second, nested).Equals(await module.Invoke(context, "probe", multiple))
                || !Nil.Value.Equals(await module.Invoke(context, "probe", new BitString([0xff]))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            var separated = new BitString([4, 1, 7, 31, 129, 1, 1, 9]);
            if (!Term.Tuple(first, second).Equals(await module.Invoke(context, "single", separated)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(context, "single", new BitString([4, 3, 0x2a]));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), new BitString([0x2a]))))
            {
            }

            return Nil.Value;
        });

        byte[] one = Opcodes(image, "one");
        byte[] parts = Opcodes(image, "parts");
        if (!one.Contains(StartBinaryMatchKnown) || one.Contains(IsTaggedTuple) || one.Contains(TrimFrame)
            || !parts.Contains(TrimFrame) || parts.Contains(IsTaggedTuple)
            || !Opcodes(unproved, "parts").Contains(IsTaggedTuple) || !Opcodes(exported, "parts").Contains(IsTaggedTuple))
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static byte[] Opcodes(byte[] image, string name)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, var arity]
                && atoms[(int)(BigInteger)Property(function, "Value")] == name
                && (int)(BigInteger)Property(arity, "Value") == 1
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        return instructions.Skip(start + 1).Select(Opcode).TakeWhile(opcode => opcode != FunctionInfo).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
