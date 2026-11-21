using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDeadStackProjectionChecks
{
    internal const string TestName = "compiler/beam/clears-unread-stack-projections";
    private const string MissingRewrite = "An unread stack projection still occupied its stack slot.";
    private const string WrongPhase = "Stack projection cleanup used the wrong phase for a function's calls.";
    private const string EnlargedFrames = "Removing unread projections enlarged a previously compacted frame.";
    private const string UnsafeRewrite = "A stack projection read on a reachable path lost its value.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Zero = 0x00;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte SecondLabel = 0x25;
    private const byte ThirdLabel = 0x35;
    private const byte Three = 0x30;

    private const string Source = """
        -module(beam_late_stack_projection).
        -export([restore/1, field/1]).
        restore({_Count, [], Prefix}) ->
            Prefix;
        restore({_Count, Suffix, Prefix}) ->
            Prefix ++ lists:reverse(Suffix).

        field(Value) ->
            case lookup(Value) of
                [[_Ignored, {_, _, Field}, {_, _, _}]] -> {field, Field};
                [] -> false
            end.

        lookup(Value) ->
            case erlang:get(Value) of
                undefined -> [];
                Rows -> Rows
            end.
        """;

    internal static Task Run()
    {
        byte[] frame = [Label, One, Allocate, Two, One];
        byte[] tupleHead = [GetTupleElement, X0, Zero, Y0];
        byte[] tupleTail = [GetTupleElement, X0, One, Y1];
        byte[] listParts = [GetList, X0, Y0, Y1];
        byte[] clearHead = [InitYRegisters, ExtendedListOperand, One, Y0];
        byte[] clearTail = [InitYRegisters, ExtendedListOperand, One, Y1];
        byte[] collect = [TestHeap, Zero, One, ExternalCall, One, Zero];
        byte[] useHead = [Move, Y0, X0];
        byte[] useTail = [Move, Y1, X0];
        byte[] finish = [Deallocate, Two, Return];
        byte[] tuple = [.. frame, .. tupleHead, .. tupleTail, .. collect, .. useHead, .. finish, EndCode];
        byte[] droppedTail = [.. frame, .. tupleHead, .. clearTail, .. collect, .. useHead, .. finish, EndCode];
        RequireRewrite(tuple, droppedTail);

        byte[] leaf = [.. frame, .. tupleHead, .. tupleTail, .. useHead, .. finish, EndCode];
        byte[] droppedLeaf = [.. frame, .. tupleHead, .. clearTail, .. useHead, .. finish, EndCode];
        RequireRewrite(leaf, droppedLeaf, requiresOrdinaryCall: false);
        if (Rewrite(tuple, requiresOrdinaryCall: false) is not null || Rewrite(leaf) is not null)
        {
            throw new InvalidOperationException(WrongPhase);
        }

        RequireRewrite(
            [.. frame, .. listParts, .. collect, .. useHead, .. finish, EndCode],
            [.. frame, GetHead, X0, Y0, .. clearTail, .. collect, .. useHead, .. finish, EndCode]
        );
        RequireRewrite(
            [.. frame, .. listParts, .. collect, .. useTail, .. finish, EndCode],
            [.. frame, GetTail, X0, Y1, .. clearHead, .. collect, .. useTail, .. finish, EndCode]
        );
        RequireRewrite(
            [.. frame, .. listParts, .. collect, .. finish, EndCode],
            [.. frame, InitYRegisters, ExtendedListOperand, Two, Y0, Y1, .. collect, .. finish, EndCode]
        );
        byte[] callingBranch = [
            .. frame, .. tupleHead, .. tupleTail, IsNil, SecondLabel, X0,
            .. useHead, .. finish, Label, Two, .. collect, .. useHead, .. finish, EndCode
        ];
        byte[] droppedOnBranches = [
            .. frame, .. tupleHead, .. clearTail, IsNil, SecondLabel, X0,
            .. useHead, .. finish, Label, Two, .. collect, .. useHead, .. finish, EndCode
        ];
        RequireRewrite(callingBranch, droppedOnBranches);
        if (Rewrite(callingBranch, requiresOrdinaryCall: false) is not null)
        {
            throw new InvalidOperationException(WrongPhase);
        }

        byte[] joined =
        [
            .. frame, .. tupleHead, IsNil, SecondLabel, X0,
            .. tupleTail, Jump, ThirdLabel, Label, Two, .. tupleTail, Label, Three,
            .. collect, .. useHead, .. finish, EndCode
        ];
        byte[] joinedExpected =
        [
            .. frame, .. tupleHead, IsNil, SecondLabel, X0,
            .. clearTail, Jump, ThirdLabel, Label, Two, .. clearTail, Label, Three,
            .. collect, .. useHead, .. finish, EndCode
        ];
        RequireRewrite(joined, joinedExpected);
        byte[] trimmed = [.. frame, .. listParts, TrimFrame, One, One, .. useHead, Deallocate, One, Return, EndCode];
        byte[] branchReadsTail =
        [
            .. frame, .. tupleHead, .. tupleTail, IsNil, SecondLabel, X0,
            .. collect, .. useHead, .. finish,
            Label, Two, .. useTail, .. finish, EndCode
        ];
        byte[] aliasedInput =
        [
            .. frame, Move, X0, Y1, GetList, Y1, Y0, Y1,
            .. collect, .. useHead, .. finish, EndCode
        ];
        byte[] bothRead =
        [
            .. frame, .. listParts, .. collect, .. useHead, Move, Y1, X1,
            .. finish, EndCode
        ];
        if (Rewrite(branchReadsTail) is not null || Rewrite(trimmed, requiresOrdinaryCall: false) is not null
            || Rewrite(aliasedInput) is not null || Rewrite(bothRead) is not null)
        {
            throw new InvalidOperationException(UnsafeRewrite);
        }

        byte[] compacted =
        [
            Label, One, Allocate, One, One, .. tupleHead, .. collect, .. useHead,
            Deallocate, One, Return, EndCode
        ];
        if (Rewrite(droppedTail, "RemoveUnusedTrailingFrameSlots") is not { } shortened
            || !shortened.AsSpan().SequenceEqual(compacted))
        {
            throw new InvalidOperationException(MissingRewrite);
        }

        RequirePreservedFrameWidths();

        return Task.CompletedTask;
    }

    private static void RequirePreservedFrameWidths()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_late_stack_projection.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        (string Function, int Limit)[] expectedFrames = [("restore", 1), ("field", 3)];
        foreach (var (name, limit) in expectedFrames)
        {
            object function = functions.Single(item => (string)Property(item, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            foreach (object instruction in instructions[start..end])
            {
                byte opcode = (byte)Property(Property(instruction, "Definition"), "Opcode");
                if (opcode is Allocate or AllocateHeap)
                {
                    object frame = ((IEnumerable)Property(instruction, "Operands")).Cast<object>().First();
                    if ((BigInteger)Property(frame, "Value") > limit)
                    {
                        throw new InvalidOperationException(EnlargedFrames);
                    }
                }
            }
        }
    }

    private static void RequireRewrite(byte[] source, byte[] expected, bool requiresOrdinaryCall = true)
    {
        if (Rewrite(source, requiresOrdinaryCall: requiresOrdinaryCall) is not { } actual
            || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingRewrite);
        }
    }

    private static byte[]? Rewrite(
        byte[] source,
        string method = "RemoveDeadStackProjections",
        bool requiresOrdinaryCall = true
    )
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            null
        );

        object[] arguments = method == "RemoveDeadStackProjections"
            ? [instructions, body, requiresOrdinaryCall]
            : [instructions, body];

        return (byte[]?)Call(typeof(BeamModuleCompiler), method, arguments);
    }
}
