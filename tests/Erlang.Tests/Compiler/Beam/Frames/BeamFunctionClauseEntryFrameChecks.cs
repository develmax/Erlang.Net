using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFunctionClauseEntryFrameChecks
{
    internal const string TestName = "compiler/beam/delays-the-frame-before-own-function-clause-failure";
    private const string MissingPlacement = "Expected read-only entry tests to fail without a frame and the call path to retain its frame.";
    private const string InvalidPlacement = "Expected a mutating, collecting, shared, or local-target failure path to retain its frame.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Y3 = 0x34;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte FunctionClause = 0x15;
    private const byte RecursiveEntry = 0x25;
    private const byte Failed = 0x35;
    private const byte LocalExit = 0x45;
    private const byte Outer = 0x32;
    private const byte Inner = 0x42;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] frame = [Allocate, One, Two, Move, X0, Y0];
        byte[] tests = [IsAtom, Failed, X1, ExactEqualTest, Failed, Y0, X1];
        byte[] delayedTests = [IsAtom, Failed, X1, ExactEqualTest, Failed, X0, X1];
        byte[] success = [TestHeap, One, Two, LocalCall, Two, RecursiveEntry, Move, Y0, X0, Deallocate, One, Return];
        byte[] failure = [Label, Three, Line, One, Deallocate, One, Jump, FunctionClause, EndCode];
        byte[] delayedFailure = [Label, Three, Line, One, Jump, FunctionClause, EndCode];
        int testStackRead = entry.Length + frame.Length + 5;
        byte[] original = [.. entry, .. frame, .. tests, .. success, .. failure];
        byte[] expected = [.. entry, .. delayedTests, .. frame, .. success, .. delayedFailure];
        if (Rewrite(original, [(testStackRead, testStackRead + 1, 0)]) is not { } actual
            || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }

        byte[] changedArguments =
        [
            .. entry, .. frame, .. tests, .. success,
            Label, Three, Move, X1, X0, Deallocate, One, Jump, FunctionClause, EndCode
        ];
        byte[] collectingFailure =
        [
            .. entry, .. frame, .. tests, .. success,
            Label, Three, TestHeap, One, Two, Deallocate, One, Jump, FunctionClause, EndCode
        ];
        byte[] sharedFailure =
        [
            .. entry, .. frame, .. tests,
            LocalCall, Two, RecursiveEntry, Move, Y0, X0, Jump, Failed, .. failure
        ];
        byte[] localTarget =
        [
            .. entry, .. frame, .. tests, .. success,
            Label, Three, Deallocate, One, Jump, LocalExit, Label, Four, Return, EndCode
        ];
        if (Rewrite(changedArguments, []) is not null || Rewrite(collectingFailure, []) is not null
            || Rewrite(sharedFailure, []) is not null || Rewrite(localTarget, []) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }

        CheckNestedRecord();

        return Task.CompletedTask;
    }

    private static void CheckNestedRecord()
    {
        byte[] entry =
        [
            Label, One, FunctionInfo, Module, Function, Two, Label, Two,
            IsTaggedTuple, FunctionClause, X0, Two, Outer
        ];
        byte[] frame =
        [
            Allocate, Four, Two, InitYRegisters, ExtendedListOperand, Two, Y2, Y3,
            Move, X0, Y0, Move, X1, Y1
        ];
        byte[] liftedFrame = [Allocate, Four, Four, .. frame[3..], Move, X3, Y2];
        byte[] tests =
        [
            GetTupleElement, Y0, One, Y2,
            IsTaggedTuple, Failed, Y2, Two, Inner,
            GetTupleElement, Y2, One, Y2,
            ExactEqualTest, Failed, ExtendedTypedRegisterOperand, Y2, One, Inner
        ];
        byte[] liftedTests =
        [
            GetTupleElement, X0, One, X2,
            IsTaggedTuple, Failed, X2, Two, Inner,
            GetTupleElement, X2, One, X3,
            ExactEqualTest, Failed, ExtendedTypedRegisterOperand, X3, One, Inner
        ];
        byte[] call = [LocalCall, Two, RecursiveEntry];
        byte[] result = [Move, Y2, X0, Deallocate, Four, Return];
        byte[] relay = [Label, Three, Move, Y1, X1, Move, Y0, X0, Deallocate, Four, Jump, FunctionClause, EndCode];
        RequireNestedRewrite(
            [.. entry, .. frame, .. tests, .. call, .. result, .. relay],
            [.. entry, .. liftedTests, .. liftedFrame, .. call, .. result, Label, Three, Jump, FunctionClause, EndCode]
        );

        byte[] lateGuard = [IsAtom, Failed, X0];
        byte[] directGuards =
        [
            GetTupleElement, X0, One, X2,
            IsTaggedTuple, FunctionClause, X2, Two, Inner,
            GetTupleElement, X2, One, X3,
            ExactEqualTest, FunctionClause, ExtendedTypedRegisterOperand, X3, One, Inner
        ];
        RequireNestedRewrite(
            [.. entry, .. frame, .. tests, .. call, .. lateGuard, .. result, .. relay],
            [.. entry, .. directGuards, .. liftedFrame, .. call, .. lateGuard, .. result, .. relay]
        );

        byte[] corruptRestore = [Label, Three, Move, Y1, X0, Deallocate, Four, Jump, FunctionClause, EndCode];
        byte[] distinctFailure =
        [
            .. entry, .. frame, .. tests[..4],
            IsTaggedTuple, LocalExit, Y2, Two, Inner, .. tests[9..],
            .. call, .. result, .. relay[..^1], Label, Four, Deallocate, Four, Return, EndCode
        ];
        byte[] unprovedField =
        [
            .. entry, .. frame, GetTupleElement, Y0, Two, Y2,
            .. tests[4..], .. call, .. result, .. relay
        ];
        if (Rewrite([.. entry, .. frame, .. tests, .. call, .. result, .. corruptRestore]) is not null
            || Rewrite(distinctFailure) is not null || Rewrite(unprovedField) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }
    }

    private static void RequireNestedRewrite(byte[] before, byte[] expected)
    {
        if (Rewrite(before) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }
    }

    private static byte[]? Rewrite(byte[] source, List<(int Start, int End, int Slot)>? stackOperands = null)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            5,
            1,
            instructions
        );
        var decodedSlots = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordSlot = (start, end, slot) => decodedSlots.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            recordSlot
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayEntryStackFrame",
            instructions,
            body,
            stackOperands ?? decodedSlots,
            null,
            new Func<object, bool>(static _ => false),
            false
        );
    }
}
