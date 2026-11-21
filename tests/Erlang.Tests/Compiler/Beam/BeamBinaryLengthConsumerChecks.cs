using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBinaryLengthConsumerChecks
{
    internal const string TestName = "compiler/beam/skips-proven-binary-length-payload";
    private const string Fixture = "beam_binary_length_consumer.erl";
    private const string WrongSchedule = "The binary length consumer did not use its expected payload skip.";
    private const string WrongResult = "The payload skip changed its result or failure reason.";

    private const string Source = """
        -module(beam_binary_length_consumer).
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
        forward(Payload) -> {indefinite,Payload}.
        strip(<<_:8,Rest/binary>>) -> {left,Rest}.
        measure(<<1:1,0:7,Tail/binary>>) -> {forever,Tail};
        measure(<<0:1,Count:7,Tail/binary>>) -> {Count,Tail};
        measure(<<1:1,Width:7,Count:Width/unit:8,Tail/binary>>) -> {Count,Tail}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unproved = CodeGeneration.CompileBeamModule(
            Source.Replace("strip(<<_:8,Rest/binary>>)", "strip(Rest)", StringComparison.Ordinal),
            Fixture
        );
        byte[] exported = CodeGeneration.CompileBeamModule(
            Source.Replace("[probe/1]", "[probe/1, measure/1]", StringComparison.Ordinal),
            Fixture
        );

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (var (data, tag) in new (byte[] Data, string Tag)[]
            {
                ([0xff, 0x00, 0x2a], "rest"),
                ([0xff, 0x02, 0x07, 0x08, 0x2a], "rest"),
                ([0xff, 0x81, 0x02, 0x07, 0x08, 0x2a], "rest"),
                ([0xff, 0x80, 0x2a], "indefinite")
            })
            {
                Term expected = Term.Tuple(Term.A(tag), new BitString([0x2a]));
                if (!expected.Equals(await module.Invoke(context, "probe", new BitString(data))))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            try
            {
                await module.Invoke(context, "probe", new BitString([0xff, 0x03, 0x2a]));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), new BitString([0x2a]))))
            {
            }

            return Nil.Value;
        });

        byte[] consumer = Opcodes(image);
        if (!consumer.Contains(StartBinaryMatchKnown) || !consumer.Contains(SkipBinaryBits)
            || !consumer.Contains(BadMatch) || Opcodes(unproved).Contains(StartBinaryMatchKnown)
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
                && atoms[(int)(BigInteger)Property(function, "Value")] == "consume"
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
