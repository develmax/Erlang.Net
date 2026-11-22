using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamJoinedRecordCallFrameChecks
{
    internal const string TestName = "compiler/beam/localizes-the-call-frame-before-a-record-join";
    private const string WrongFrame = "A checked record join used unexpected call spills or heap roots.";
    private const string WrongValue = "Localizing a record join changed a returned record.";
    private const string Fixture = "beam_joined_record_return_probe.erl";
    private const string Source = """
        -module(beam_joined_record_return_probe).
        -export([grow/1, barrier/1]).
        -record(bucket, {count, marker, shift, extra}).
        grow(#bucket{count = Count, marker = Prev, shift = Shift} = R) ->
            Advanced = Prev + 1,
            Count1 = Count + 1,
            Result = case scale(Shift) + 1 of
                Check when Check == Advanced ->
                    R#bucket{count = Count1, marker = 1, shift = Shift + 1};
                _ ->
                    R#bucket{count = Count1, marker = Advanced}
            end,
            {Result,[Prev],[Count1]};
        grow(Previous) ->
            State = convert(Previous),
            grow(State).

        barrier(#bucket{count = Count, marker = Prev, shift = Shift} = R) ->
            Advanced = Prev + 1,
            Count1 = Count + 1,
            Result = case scale(Shift) + 1 of
                Check when Check == Advanced ->
                    R#bucket{count = Count1, marker = 1, shift = Shift + 1};
                _ ->
                    R#bucket{count = Count1, marker = Advanced}
            end,
            erlang:put(state,Result),
            {Result,[Prev],[Count1]};
        barrier(Previous) ->
            State = convert(Previous),
            barrier(State).

        convert({bucket, Count, Marker, Shift}) ->
            #bucket{count = Count, marker = Marker, shift = Shift}.
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
            Term marker = Term.I(4);
            Term spare = Term.A("spare");

            Term Record(int position, int level) => Term.Tuple(
                Term.A("bucket"),
                Term.I(4),
                Term.I(position),
                Term.I(level),
                spare
            );

            Term Input(int position) => Term.Tuple(
                Term.A("bucket"),
                Term.I(3),
                Term.I(position),
                Term.I(2),
                spare
            );
            Term hot = Term.Tuple(Record(1, 3), Term.List(marker), Term.List(Term.I(4)));
            Term cold = Term.Tuple(Record(3, 2), Term.List(Term.I(2)), Term.List(Term.I(4)));
            if (!hot.Equals(await module.Invoke(context, "grow", Input(4)))
                || !cold.Equals(await module.Invoke(context, "grow", Input(2)))
                || !hot.Equals(await module.Invoke(context, "barrier", Input(4))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }

    private static void CheckFrames(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] grow = Body("grow");
        int allocation = Array.FindIndex(grow, instruction => Opcode(instruction) == Allocate);
        if (allocation < 0
            || !Values(grow[allocation]).SequenceEqual([5, 4])
            || !grow.Take(allocation).Where(instruction => Opcode(instruction) == GcBifTwo)
                .Select(instruction => Values(instruction).ElementAt(1)).SequenceEqual([2, 4])
            || Opcode(grow[allocation + 1]) != Move
            || !Values(grow[allocation + 1]).SequenceEqual([3, 1])
            || Opcode(grow[allocation + 2]) != Move
            || !Values(grow[allocation + 2]).SequenceEqual([2, 2])
            || !grow.Where(instruction => Opcode(instruction) == TestHeap)
                .Select(instruction => Values(instruction).Last()).SequenceEqual([1, 0, 1])
            || !Body("barrier").Any(instruction => Opcode(instruction) == ExternalCall))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<int> Values(object instruction) => Children(instruction, "Operands").Select(operand => (int)Value(operand));

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
