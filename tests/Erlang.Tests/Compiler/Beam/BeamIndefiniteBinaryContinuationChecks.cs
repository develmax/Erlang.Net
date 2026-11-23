using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamIndefiniteBinaryContinuationChecks
{
    internal const string TestName = "compiler/beam/resumes-closed-indefinite-binary-continuation";
    private const string Fixture = "beam_indefinite_binary_continuation.erl";
    private const string WrongSchedule = "The indefinite binary loop did not use its expected closed continuation.";
    private const string WrongResult = "The indefinite binary loop changed its result or failure reason.";

    private const string Source = """
        -module(beam_indefinite_binary_continuation).
        -export([probe/1]).
        probe(Bits) ->
            {left,Tail} = strip(Bits),
            consume(Tail).
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
            Source.Replace(
                "tag_bytes(<<0:1,_:7,Rest/binary>>) -> {rest,Rest}",
                "tag_bytes(<<0:1,_:7,Rest/binary>>) -> {other,Rest}",
                StringComparison.Ordinal
            ),
            Fixture
        );
        byte[] exported = CodeGeneration.CompileBeamModule(
            Source.Replace("[probe/1]", "[probe/1, forward/1]", StringComparison.Ordinal),
            Fixture
        );

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (byte[] data in new byte[][]
            {
                [0xff, 0x80, 0, 0, 0x2a],
                [0xff, 0x80, 4, 1, 7, 0, 0, 0x2a],
                [0xff, 0x80, 31, 129, 1, 0, 0, 0, 0x2a],
                [0xff, 0x80, 48, 128, 4, 1, 7, 0, 0, 0, 0, 0x2a]
            })
            {
                Term expected = Term.Tuple(Term.A("rest"), new BitString([0x2a]));
                if (!expected.Equals(await module.Invoke(context, "probe", new BitString(data))))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            try
            {
                await module.Invoke(context, "probe", new BitString([0xff, 0x80, 4, 2, 0x2a]));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), new BitString([0x2a]))))
            {
            }

            return Nil.Value;
        });

        byte[] loop = Opcodes(image);
        if (!loop.Contains(StartBinaryMatchKnown) || !loop.Contains(SetBinaryPosition)
            || loop.Contains(IsTaggedTuple) || Opcodes(unproved).Contains(StartBinaryMatchKnown)
            || Opcodes(exported).Contains(StartBinaryMatchKnown))
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static byte[] Opcodes(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, var arity]
                && atoms[(int)(BigInteger)Property(function, "Value")] == "forward"
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
