using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFlatTaggedEntryChecks
{
    internal const string TestName = "compiler/beam/shares-tuple-projections-before-tagged-entry-frames";
    private const string Fixture = "beam_flat_tagged_entry.erl";
    private const string WrongGuard = "The grouped guard must test its secondary record and return before allocating a frame.";
    private const string Missing = "The compiled tagged entry function is missing.";
    private const string WrongDispatch = "The shared field reads and tag switch must precede the branch frames.";
    private const string WrongRead = "A field needed in both disjoint arms should be projected once at their common entry.";

    internal const string Source = """
        -module(beam_flat_tagged_entry).
        -export([dispatch/2, repeat_tag/2, guarded/2]).
        -record(state, {key}).

        dispatch({alpha, Stamp, Field, Payload}, State) ->
            beam_flat_entry_target:observe(Stamp, Field),
            {kept, Payload, State};
        dispatch({zulu, Stamp, process, Payload}, State) ->
            beam_flat_entry_target:observe(Stamp, Payload),
            {kept, Payload, State};
        dispatch({short, Stamp}, State) ->
            {simple, Stamp, State};
        dispatch(Other, State) ->
            {fallback, Other, State}.

        repeat_tag({alpha, Stamp, short}, State) ->
            {short, Stamp, State};
        repeat_tag({alpha, Stamp, long}, State) ->
            {long, Stamp, State};
        repeat_tag({single, Stamp}, State) ->
            {single, Stamp, State};
        repeat_tag(Other, State) ->
            {fallback, Other, State}.

        guarded({halt, Key, Reason}, State) when Key == State#state.key ->
            {halted, Reason, State};
        guarded({old, Value}, State) ->
            {legacy, Value, State};
        guarded(Other, State) ->
            {fallback, Other, State}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "dispatch" && Value(Operands(instruction)[2]) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(Missing);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int arity = Array.FindIndex(body, instruction => Opcode(instruction) == SelectTupleArity);
        int selection = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        if (arity < 0 || selection <= arity || body.Take(selection).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        int[] shared = body.Skip(arity + 1).Take(selection - arity - 1)
            .Where(instruction => Opcode(instruction) == GetTupleElement).Select(Operands)
            .Where(operands => Property(operands[0], "Kind") is "x" && Value(operands[0]) == 0)
            .Select(operands => Value(operands[1])).ToArray();
        if (!shared.SequenceEqual([0, 1, 2, 3]))
        {
            throw new InvalidOperationException(WrongRead);
        }

        int guard = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "guarded" && Value(Operands(instruction)[2]) == 2
        );
        if (guard < 0)
        {
            throw new InvalidOperationException(Missing);
        }

        object[] firstArm = instructions.Skip(guard + 1).TakeWhile(instruction => Opcode(instruction) != Return).ToArray();
        if (!firstArm.Any(instruction => Opcode(instruction) == IsTaggedTuple)
            || !firstArm.Any(instruction => Opcode(instruction) == NumericEqualTest)
            || firstArm.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
        {
            throw new InvalidOperationException(WrongGuard);
        }

        return Task.CompletedTask;
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
