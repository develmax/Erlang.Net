using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedRecursiveEntrySlotChecks
{
    internal const string TestName = "compiler/beam/reserves-closed-recursive-entry-slots-in-argument-order";
    internal const string Fixture = "beam_closed_recursive_entry_slots_probe.erl";
    private const string WrongSlots = "The checked recursive entry frame did not preserve the expected parameter custody.";
    private const string XRegister = "x";
    private const string YRegister = "y";

    internal const string Source = """
        -module(beam_closed_recursive_entry_slots_probe).
        -export([run/1, open/2]).

        run(Parent) -> retry(Parent, 3).

        retry(_, 0) -> exit(done);
        retry(Parent, Left) ->
            case erlang:whereis(beam_closed_recursive_entry_slots_probe) of
                undefined -> {ready, Parent, Left};
                Pid ->
                    Pid ! Parent,
                    retry(Parent, Left - 1)
            end.

        open(_, 0) -> exit(done);
        open(Parent, Left) ->
            case erlang:whereis(beam_closed_recursive_entry_slots_probe) of
                undefined -> {ready, Parent, Left};
                Pid ->
                    Pid ! Parent,
                    open(Parent, Left - 1)
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = GetItems(code, "Instructions");
        string[] atoms = GetItems(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasFirstFramePair("retry", 1, 0) || !HasFirstFramePair("open", 0, 1))
        {
            throw new InvalidOperationException(WrongSlots);
        }

        return Task.CompletedTask;

        bool HasFirstFramePair(string name, int first, int second)
        {
            int function = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && GetItems(instruction, "Operands") is [_, var atom, _]
                    && atoms[(int)Value(atom)] == name
            );
            if (function < 0)
            {
                return false;
            }

            for (int index = function + 1; index + 2 < instructions.Length; index++)
            {
                byte opcode = Opcode(instructions[index]);
                if (opcode == FunctionInfo)
                {
                    return false;
                }

                if (opcode == Allocate)
                {
                    return IsCopy(instructions[index + 1], first, 0) && IsCopy(instructions[index + 2], second, 1);
                }
            }

            return false;
        }
    }

    private static bool IsCopy(object instruction, int input, int slot) =>
        Opcode(instruction) == Move && GetItems(instruction, "Operands") is [var source, var destination]
        && (string)Property(source, "Kind") == XRegister && Value(source) == input
        && (string)Property(destination, "Kind") == YRegister && Value(destination) == slot;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] GetItems(object owner, string property) => ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
