using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedEntryEffectChecks
{
    internal const string TestName = "compiler/beam/checks-nested-entry-before-single-effect-allocation";
    private const string Frame = "Check nested source patterns before allocating a one-slot effect frame.";
    private const string Scratch = "Keep a live incoming tuple until all outgoing arguments have been prepared.";

    private const string Source = """
        -module(beam_checked_entry_effect).
        -export([route/2, retain_input/2, guarded/2, repeated/2]).

        -record(state, {config}).

        route({envelope, _Ignored, {event, Value}}, State = #state{}) ->
            service:report(State#state.config, shown, [Value]),
            {ok, State}.

        retain_input({envelope, Key, {event, Value}}, State = #state{}) ->
            service:report(Key, shown, [Value]),
            {ok, State}.

        guarded({envelope, _Ignored, {event, Value}}, State = #state{}) when is_integer(Value) ->
            service:report(State#state.config, shown, [Value]),
            {ok, State}.

        repeated({envelope, Value, {event, Value}}, State = #state{}) ->
            service:report(State#state.config, shown, [Value]),
            {ok, State}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_checked_entry_effect.erl"));
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        object[] normal = Body("route");
        int allocation = Array.FindIndex(normal, instruction => Opcode(instruction) == AllocateHeap);
        if (allocation < 0 || normal[..allocation].Count(instruction => Opcode(instruction) == IsTaggedTuple) != 3
            || (BigInteger)Property(Items(normal[allocation], "Operands")[0], "Value") != 1
            || normal.Any(instruction => Opcode(instruction) == TrimFrame))
        {
            throw new InvalidOperationException(Frame);
        }

        object[] retained = Body("retain_input");
        int use = Array.FindIndex(retained, instruction => Opcode(instruction) == PutList);
        if (use < 1 || !IsX(Items(retained[use], "Operands")[0], 2)
            || !retained[(use + 1)..].Any(instruction =>
                Opcode(instruction) == GetTupleElement && IsX(Items(instruction, "Operands")[0], 0)))
        {
            throw new InvalidOperationException(Scratch);
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static bool IsX(object operand, int index) =>
        (string)Property(operand, "Kind") == "x" && (BigInteger)Property(operand, "Value") == index;

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
