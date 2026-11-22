using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTwoAtomEntrySelectionChecks
{
    internal const string TestName = "compiler/beam/selects-two-atoms-in-one-entry-column";

    private const string Source = """
        -module(two_atom_entry_fixture).
        -export([choose/2,member/2,permute/3,one_arm/2,guarded/2,with_list/3,with_unproved/3]).

        choose(second, X) ->
            two_atom_entry_provider:effect(X);
        choose(first, _) ->
            constant.

        member(_, open) ->
            accepted;
        member(_, closed) ->
            accepted.

        permute(X, second, Y) ->
            pack(Y, X);
        permute(X, first, Y) ->
            pack(X, Y).

        one_arm(chosen, _) ->
            selected;
        one_arm(_, _) ->
            fallback.

        guarded(a, X) when is_integer(X) ->
            selected;
        guarded(z, _) ->
            other.

        with_list(none, _, _) -> [];
        with_list(all, Value, Tail) ->
            case produce(Value, [prefix | Tail]) of
                [] -> [];
                Result -> [{header, 1} | Result]
            end.

        with_unproved(none, _, _) -> [];
        with_unproved(all, Value, Tail) ->
            case unknown(Value, [prefix | Tail]) of
                [] -> [];
                Result -> [{header, 1} | Result]
            end.

        produce([], _) -> [];
        produce([Head | _], _) -> [Head].
        unknown(Value, _) -> Value.

        pack(X, Y) ->
            {X, Y}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "two_atom_entry_fixture.erl"));
        VerifySelection(
            code,
            "choose",
            0,
            sharesBody: false
        );
        VerifySelection(
            code,
            "member",
            1,
            sharesBody: true
        );
        VerifySelection(
            code,
            "permute",
            1,
            sharesBody: false
        );

        VerifySelection(
            code,
            "with_list",
            0,
            sharesBody: false
        );
        VerifyOwnedListCall(code);

        byte[] oneArm = Instructions(code, "one_arm").Select(Opcode).ToArray();
        byte[] guarded = Instructions(code, "guarded").Select(Opcode).ToArray();
        if (!oneArm.Contains(ExactEqualTest) || oneArm.Contains(SelectValue) || !guarded.Contains(IsInteger))
        {
            throw new InvalidOperationException(BeamTwoAtomEntrySelectionDiagnostics.Fallback);
        }
    }

    private static void VerifyOwnedListCall(object code)
    {
        object[] body = Instructions(code, "with_list");
        object? frame = body.SingleOrDefault(instruction => Opcode(instruction) == AllocateHeap);
        object? check = body.SingleOrDefault(instruction => Opcode(instruction) == IsNil);
        object? call = body.SingleOrDefault(instruction => Opcode(instruction) == LocalCall);
        object producer = Items(code, "Functions").Single(function => (string)Property(function, "Name") == "produce");
        if (call is null || Value(Items(call, "Operands")[1]) != (int)Property(producer, "EntryLabel")
            || frame is null || !Items(frame, "Operands").Select(Value).SequenceEqual([0, 2, 3])
            || check is null || Items(check, "Operands") is not [_, var result]
            || (string)Property(result, "Kind") != "x" || Value(result) != 0
            || Property(result, "TypeIndex") is null
            || Instructions(code, "with_unproved").Any(instruction => Opcode(instruction) == IsNil
                && Items(instruction, "Operands") is [_, var unproved] && Property(unproved, "TypeIndex") is not null))
        {
            throw new InvalidOperationException(BeamTwoAtomEntrySelectionDiagnostics.Layout);
        }
    }

    private static void VerifySelection(
        object code,
        string function,
        int selector,
        bool sharesBody
    )
    {
        object[] instructions = Instructions(code, function);
        object? selected = instructions.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
        if (selected is null || instructions.Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(BeamTwoAtomEntrySelectionDiagnostics.Table);
        }

        object[] operands = Items(selected, "Operands");
        if (operands.Length != 7 || (string)Property(operands[0], "Kind") != "x" || Value(operands[0]) != selector
            || operands[4].Equals(operands[6]) != sharesBody)
        {
            throw new InvalidOperationException(BeamTwoAtomEntrySelectionDiagnostics.Table);
        }

        if (!sharesBody && LabelPosition(operands[6]) >= LabelPosition(operands[4]))
        {
            throw new InvalidOperationException(BeamTwoAtomEntrySelectionDiagnostics.Layout);
        }

        int LabelPosition(object operand) => Array.FindIndex(instructions, instruction =>
            Opcode(instruction) == Label && Value(Items(instruction, "Operands")[0]) == Value(operand));
    }

    private static object[] Instructions(object code, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions")[start..end];
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
