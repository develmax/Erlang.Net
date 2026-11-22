using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryMatchedMapUpdateChecks
{
    internal const string TestName = "compiler/beam/reuses-matched-map-owners-across-calls";
    private const string Fixture = "beam_entry_matched_map_update_probe.erl";
    private const string UnexpectedMapPlan = "Expected entry-proven maps to update directly while unknown maps keep their tests.";

    internal const string Source = """
        -module(beam_entry_matched_map_update_probe).
        -export([forward/1, matched/2, dynamic/2, exact/2, remember/1, constructed/2, branch/3,
                 joined/3, uncertain/3]).

        forward(Value) -> after_map(Value).

        matched(#{ready := Old} = State, Value) ->
            A = ?MODULE:remember(Old),
            B = ?MODULE:remember(Value),
            {reply,State#{marker => A,ready => B}};
        matched(Value, _) ->
            {other,Value}.

        dynamic(State, Value) ->
            B = ?MODULE:remember(Value),
            {reply,State#{ready => B}}.

        exact(#{ready := Old} = State, Value) ->
            ?MODULE:remember(Old),
            State#{missing := Value}.

        constructed(Value, Flag) ->
            State = #{ready => Value},
            case Flag of
                keep -> State;
                update -> State#{ready => {updated,Value}}
            end.

        branch(State, Value, Which) ->
            case Which of
                fresh ->
                    Built = #{ready => Value},
                    Built#{ready => 1};
                unknown -> State#{ready => Value}
            end.

        joined(Which, Value, Extra) ->
            Seed = #{ready => Value},
            Result = case Which of
                keep -> Seed;
                _ -> Seed#{updated => Value}
            end,
            Result#{next => ?MODULE:remember(Extra)}.

        uncertain(Which, Value, Unknown) ->
            Seed = #{ready => Value},
            Result = case Which of
                keep -> Seed;
                _ -> Unknown
            end,
            Result#{next => ?MODULE:remember(Value)}.

        remember(Value) -> self() ! {effect,Value}, Value.

        after_map(Value) -> {retained,?MODULE:remember(Value)}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] matched = FunctionInstructions("matched");
        object[] dynamic = FunctionInstructions("dynamic");
        object? update = matched.FirstOrDefault(instruction => Opcode(instruction) == PutMapAssoc);
        if (update is null || !Equals(Property(Items(update, "Operands").ElementAt(1), "Kind"), "y")
            || matched.Count(instruction => Opcode(instruction) == IsMap) != 1
            || !dynamic.Any(instruction => Opcode(instruction) == IsMap)
            || FunctionInstructions("constructed").Any(instruction => Opcode(instruction) == IsMap)
            || FunctionInstructions("branch").Count(instruction => Opcode(instruction) == IsMap) != 1
            || FunctionInstructions("joined").Any(instruction => Opcode(instruction) == IsMap)
            || FunctionInstructions("uncertain").Count(instruction => Opcode(instruction) == IsMap) != 1
            || !CallsForwardEntry())
        {
            throw new InvalidOperationException(UnexpectedMapPlan);
        }

        bool CallsForwardEntry()
        {
            object[] arguments = FunctionInstructions("after_map");
            if (arguments.Length == 0 || Opcode(arguments[0]) != Label)
            {
                return false;
            }

            int entry = Value(Items(arguments[0], "Operands").First());

            return FunctionInstructions("forward").Any(instruction =>
                Opcode(instruction) is LocalCall or TailLocalCall or TailLocalCallWithFrame
                && Value(Items(instruction, "Operands").ElementAt(1)) == entry);
        }

        object[] FunctionInstructions(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
