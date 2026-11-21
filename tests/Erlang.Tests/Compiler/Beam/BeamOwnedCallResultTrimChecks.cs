using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedCallResultTrimChecks
{
    internal const string TestName = "compiler/beam/trims-an-owned-call-result-branch";
    private const string Fixture = "beam_owned_call_result_trim_probe.erl";
    private const string WrongFrame = "The selected branch used the wrong call frame or stack roots.";
    private const string WrongResult = "Trimming a call result branch changed its return value.";

    private const string Source = """
        -module(beam_owned_call_result_trim_probe).
        -export([bucket/2, retain/2]).
        -record(bucket, {limit, hole, shift, engine}).
        
        bucket(#bucket{engine = alpha, limit = Lim, shift = Shift}, Key) ->
            Value = erlang:phash(Key, scale(Shift + 1)),
            if Value > Lim -> Value - scale(Shift); true -> Value end;
        bucket(#bucket{engine = beta, limit = Lim, shift = Shift}, Key) ->
            Value = erlang:phash2(Key, scale(Shift + 1)) + 1,
            if Value > Lim -> Value - scale(Shift); true -> Value end;
        bucket(Previous, Key) ->
            Value = convert(Previous),
            bucket(Value, Key).
        
        retain(#bucket{engine = alpha, limit = Lim, shift = Shift}, Key) ->
            Value = erlang:phash(Key, scale(Shift + 1)),
            if Value > Lim -> Value - scale(Shift) + Lim; true -> Value end;
        retain(#bucket{engine = beta, limit = Lim, shift = Shift}, Key) ->
            Value = erlang:phash2(Key, scale(Shift + 1)) + 1,
            if Value > Lim -> Value - scale(Shift); true -> Value end;
        retain(Previous, Key) ->
            Value = convert(Previous),
            retain(Value, Key).
        
        convert({bucket, Lim, Hole, Shift}) ->
            #bucket{limit = Lim, hole = Hole, shift = Shift, engine = alpha}.
        scale(Shift) -> 1 bsl Shift.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckFrames(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.I(42);
            if (!Term.I(3).Equals(await module.Invoke(
                context,
                "bucket",
                Input("alpha", 0),
                key
            ))
                || !Term.I(7).Equals(await module.Invoke(
                    context,
                    "bucket",
                    Input("alpha", 100),
                    key
                ))
                || !Term.I(-3).Equals(await module.Invoke(
                    context,
                    "bucket",
                    Input("beta", 0),
                    key
                ))
                || !Term.I(1).Equals(await module.Invoke(
                    context,
                    "bucket",
                    Input("beta", 100),
                    key
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static Term Input(string kind, int limit) =>
        Term.Tuple(
            Term.A("bucket"),
            Term.I(limit),
            Term.A("none"),
            Term.I(2),
            Term.A(kind)
        );

    private static void CheckFrames(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("bucket");
        object[] trims = positive.Where(instruction => Opcode(instruction) == TrimFrame).ToArray();
        if (trims.Length != 2 || trims.Any(instruction => !Values(instruction).SequenceEqual([2, 1]))
            || positive.Count(instruction => Opcode(instruction) == InitYRegisters
                && Values(instruction).SequenceEqual([1, 2])) != 2
            || Body("retain").Any(instruction => Opcode(instruction) == TrimFrame))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] Body(string function)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == function
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static IEnumerable<int> Values(object instruction) => Children(instruction, "Operands").Select(operand => (int)Value(operand));

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
