using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedEntryTupleSlotChecks
{
    internal const string TestName = "compiler/beam/stores-proven-entry-tuple-field-in-frame";
    private const string MissingFieldStore = "The checked tuple must remain the allocation root until its field is stored.";
    private const string MovedGuardField = "A field read by a clause guard must precede the guard and frame.";
    private const string Fixture = "beam_checked_entry_tuple_slot.erl";
    private const string Source = """
        -module(beam_checked_entry_tuple_slot).
        -export([cancel/1, checked/1]).

        cancel({timer, Ref}) ->
            case erlang:cancel_timer(Ref) of
                Remaining when is_integer(Remaining) -> {cancelled, Remaining};
                _ -> {error, {expired, Ref}}
            end;
        cancel(infinity) ->
            ok;
        cancel(BadRef) ->
            {error, {bad_ref, BadRef}}.


        checked({timer, Ref}) when is_reference(Ref) ->
            case erlang:cancel_timer(Ref) of
                Remaining when is_integer(Remaining) -> {cancelled, Remaining};
                _ -> {error, {expired, Ref}}
            end;
        checked(BadRef) ->
            {error, {bad_ref, BadRef}}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions");
        object[] body = Body("cancel");
        int tag = Array.FindIndex(body, instruction => Opcode(instruction) == IsTaggedTuple);
        if (tag < 0 || Opcode(body[tag + 1]) != Allocate
            || !Items(body[tag + 1], "Operands").Select(Value).SequenceEqual([1, 1])
            || Opcode(body[tag + 2]) != GetTupleElement
            || Items(body[tag + 2], "Operands") is not [var tuple, var field, var slot]
            || !Register(tuple, "x", 0) || Value(field) != 1 || !Register(slot, "y", 0))
        {
            throw new InvalidOperationException(MissingFieldStore);
        }

        object[] checkedBody = Body("checked");
        int check = Array.FindIndex(checkedBody, instruction => Opcode(instruction) == IsReference);
        if (check < 0 || !checkedBody.Take(check).Any(instruction => Opcode(instruction) == GetTupleElement)
            || checkedBody.Take(check).Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
        {
            throw new InvalidOperationException(MovedGuardField);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static object[] Items(object value, string property) => ((IEnumerable)Property(value, property)).Cast<object>().ToArray();

    private static byte Opcode(object value) => (byte)Property(Property(value, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static bool Register(object operand, string kind, int index) => (string)Property(operand, "Kind") == kind && Value(operand) == index;
}
