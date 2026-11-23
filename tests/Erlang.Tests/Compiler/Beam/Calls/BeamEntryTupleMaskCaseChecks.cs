using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryTupleMaskCaseChecks
{
    internal const string TestName = "compiler/beam/uses-entry-registers-for-fields-dead-after-the-mask-guard";
    private const string Source = """
        -module(beam_entry_tuple_mask_case).
        -export([checked/4, observed/4]).

        checked(Key = {A,B,C,D}, Pos, Rows, State)
          when ((A bor B bor C bor D) band bnot 255) =:= 0, (Pos band bnot 65535) =:= 0 ->
            case lists:keyfind(Key, Pos, Rows) of
                {ok, Result} -> {Result, State};
                Other -> Other
            end.

        observed(Key = {A,B,C,D}, Pos, Rows, State)
          when ((A bor B bor C bor D) band bnot 255) =:= 0, (Pos band bnot 65535) =:= 0 ->
            case lists:keyfind(Key, Pos, Rows) of
                {ok, A} -> {matched, State};
                Other -> Other
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_entry_tuple_mask_case.erl"));
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] checkedArm = ReadBody("checked");
        int frame = Array.FindIndex(checkedArm, op => Opcode(op) is Allocate or AllocateHeap);
        if (frame < 0 || checkedArm.Take(frame).Count(op => Opcode(op) == GcBifTwo) != 5
            || checkedArm.Take(frame).Count(op => Opcode(op) == GetTupleElement) != 4
            || checkedArm.Take(frame).SelectMany(op => Items(op, "Operands"))
                .Any(operand => (string)Property(operand, "Kind") == "y"))
        {
            throw new InvalidOperationException("The entry mask must leave the caller registers intact until the body frame.");
        }

        object[] observed = ReadBody("observed");
        int observedFrame = Array.FindIndex(observed, op => Opcode(op) is Allocate or AllocateHeap);
        if (observedFrame < 0 || observed.Take(observedFrame).Count(op => Opcode(op) == GcBifTwo) == 5)
        {
            throw new InvalidOperationException("A tuple field used by a later bound match must keep its source binding.");
        }

        return Task.CompletedTask;

        object[] ReadBody(string name)
        {
            int info = Array.FindIndex(
                instructions,
                op => Opcode(op) == FunctionInfo && atoms[Value(Items(op, "Operands")[1])] == name
            );

            return instructions.Skip(info + 1).TakeWhile(op => Opcode(op) != FunctionInfo).ToArray();
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
