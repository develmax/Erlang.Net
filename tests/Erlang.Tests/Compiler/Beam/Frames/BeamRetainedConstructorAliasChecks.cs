using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedConstructorAliasChecks
{
    internal const string TestName = "compiler/beam/uses-available-x-in-constructor-with-retained-spill";
    private const string MissingAlias = "Expected constructor reads to use the incoming X alias while retaining the Y spill.";
    private const string InvalidAlias = "A constructor used its destination or an X alias lost by a branch, call, or heap check.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte Atom = 0x12;
    private const byte Four = 0x40;
    private const byte Y0 = 0x04;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Five = 0x50;
    private const byte Merge = 0x35;
    private const byte Nil = 0x02;

    internal static Task Run()
    {
        byte[] prefix = [Label, One, AllocateHeap, One, Five, Two, Move, X1, Y0];
        byte[] tuple = [PutTuple, X0, ExtendedListOperand, Two, Y0, X0];
        byte[] list = [PutList, Y0, Nil, X0];
        byte[] call = [ExternalCall, One, Zero];
        byte[] afterCall = [TestHeap, Three, One, .. tuple, Deallocate, One, Return, EndCode];
        byte[] candidate = [.. prefix, .. tuple, .. list, .. call, .. afterCall];
        byte[] expected =
        [
            .. prefix,
            PutTuple, X0, ExtendedListOperand, Two, X1, X0,
            PutList, X1, Nil, X0,
            .. call, .. afterCall
        ];
        if (Rewrite(candidate) is not { } optimized || !optimized.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingAlias);
        }

        byte[] joined = [.. prefix, IsNil, Merge, X0, Move, X0, X1, Label, Three, .. tuple, .. call, .. afterCall];
        byte[] clobbered = [.. prefix, .. call, .. afterCall];
        byte[] collected = [.. prefix, TestHeap, Three, One, .. tuple, .. call, .. afterCall];
        byte[] destinationAlias =
        [
            .. prefix,
            PutTuple, X1, ExtendedListOperand, Two, Y0, X0,
            Move, X1, X0,
            .. call, .. afterCall
        ];
        if (Rewrite(joined) is not null || Rewrite(clobbered) is not null
            || Rewrite(collected) is not null || Rewrite(destinationAlias) is not null)
        {
            throw new InvalidOperationException(InvalidAlias);
        }

        CheckEarlyArgumentMotion();
        CheckStraightCallArgumentConsumers();
        CheckSendArgumentMotion();
        CheckTrailingOrdinaryArgumentMotion();
        CheckPostClosureArgumentMotion();

        return Task.CompletedTask;
    }

    private static void CheckEarlyArgumentMotion()
    {
        byte[] prefix = [Label, One, AllocateHeap, One, Three, Three];
        byte[] initialize = [InitYRegisters, ExtendedListOperand, One, Y0];
        byte[] list = [PutList, X2, Nil, X1];
        byte[] finish = [ExternalCall, Two, Zero, Deallocate, One, Return, EndCode];
        byte[] listCandidate = [.. prefix, Move, X1, Y0, .. list, Move, Y0, X0, .. finish];
        byte[] listExpected = [.. prefix, .. initialize, Move, X1, X0, .. list, .. finish];
        byte[] tuple = [PutTuple, X0, ExtendedListOperand, Two, X1, X2];
        byte[] tupleBlock = [Move, Atom, X2, .. tuple, Move, X0, X2];
        byte[] tupleFinish = [ExternalCall, Four, Zero, Deallocate, One, Return, EndCode];
        byte[] tupleCandidate = [.. prefix, Move, X2, Y0, .. tupleBlock, Move, Y0, X3, .. tupleFinish];
        byte[] tupleExpected = [.. prefix, .. initialize, Move, X2, X3, .. tupleBlock, .. tupleFinish];
        if (Rewrite(listCandidate, removeSpills: true) is not { } movedList || !movedList.AsSpan().SequenceEqual(listExpected)
            || Rewrite(tupleCandidate, removeSpills: true) is not { } movedTuple || !movedTuple.AsSpan().SequenceEqual(tupleExpected))
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.MissingMotion);
        }

        byte[] collected = [.. prefix, Move, X1, Y0, .. list, TestHeap, Three, Two, Move, Y0, X0, .. finish];
        byte[] readDestination = [.. prefix, Move, X1, Y0, PutList, X0, Nil, X1, Move, Y0, X0, .. finish];
        byte[] writeDestination = [.. prefix, Move, X1, Y0, .. list, Move, Atom, X0, Move, Y0, X0, .. finish];
        byte[] suffix = [Move, X2, X1, Move, Y0, X0, Line, One];
        byte[] ordinary = [.. prefix, Move, X1, Y0, .. suffix, .. finish];
        byte[] ordinaryExpected = [.. prefix, .. initialize, Move, X1, X0, Move, X2, X1, Line, One, .. finish];
        if (Rewrite(ordinary, removeSpills: true) is not { } movedOrdinary
            || !movedOrdinary.AsSpan().SequenceEqual(ordinaryExpected))
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.MissingMotion);
        }

        byte[] tailOnly = [.. prefix, Move, X1, Y0, .. suffix, TailExternalCallWithFrame, Two, Zero, One, EndCode];
        byte[] unread =
        [
            .. prefix, Move, X1, Y0, .. suffix,
            ExternalCall, Zero, Zero, Deallocate, One, Return, EndCode
        ];
        byte[] priorGc = [.. prefix, Move, X1, Y0, TestHeap, Three, Three, .. suffix, .. finish];
        if (Rewrite(collected, removeSpills: true) is not null || Rewrite(readDestination, removeSpills: true) is not null
            || Rewrite(writeDestination, removeSpills: true) is not null || Rewrite(tailOnly, removeSpills: true) is not null
            || Rewrite(unread, removeSpills: true) is not null || Rewrite(priorGc, removeSpills: true) is not null)
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.UnsafeMotion);
        }
    }

    private static void CheckTrailingOrdinaryArgumentMotion()
    {
        const byte X4 = 0x43;
        byte[] entry = [Label, One, Allocate, One, Five];
        byte[] save = [Move, X2, Y0];
        byte[] reload = [Move, X3, X2, Move, Y0, X1];
        byte[] trailing = [Move, X4, X3, Line, One];
        byte[] finish = [ExternalCall, Four, Zero, Deallocate, One, Return, EndCode];
        byte[] candidate = [.. entry, .. save, .. reload, .. trailing, .. finish];
        byte[] expected =
        [
            .. entry, InitYRegisters, ExtendedListOperand, One, Y0,
            Move, X2, X1, Move, X3, X2, .. trailing, .. finish
        ];
        byte[] clobber = [.. entry, .. save, .. reload, Move, X4, X1, Line, One, .. finish];
        byte[] collect = [.. entry, .. save, .. reload, .. trailing, TestHeap, One, Four, .. finish];
        if (Rewrite(candidate, removeSpills: true) is not { } moved || !moved.AsSpan().SequenceEqual(expected)
            || Rewrite(clobber, removeSpills: true) is not null || Rewrite(collect, removeSpills: true) is not null)
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.UnsafeMotion);
        }
    }

    private static void CheckPostClosureArgumentMotion()
    {
        const byte Y1 = 0x14;
        byte[] entry = [Label, One, AllocateHeap, Two, Three, Three];
        byte[] save = [Move, X1, Y1];
        byte[] initialize = [InitYRegisters, ExtendedListOperand, One, Y1];
        byte[] closure = [MakeFun, Zero, Y0, ExtendedListOperand, One, X0];
        byte[] overwrite = [Move, X2, X1];
        byte[] reload = [Move, Y1, X0, Line, One];
        byte[] consume = [ExternalCall, Two, Zero];
        byte[] finish = [Move, Y0, X1, Deallocate, Two, Return, EndCode];
        byte[] candidate = [.. entry, .. save, .. closure, .. overwrite, .. reload, .. consume, .. finish];
        byte[] expected =
        [
            .. entry, .. initialize, .. closure,
            Move, X1, X0, .. overwrite, Line, One, .. consume, .. finish
        ];
        if (Rewrite(candidate, removeSpills: true) is not { } moved || !moved.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.MissingMotion);
        }

        byte[] unconsumed =
        [
            .. entry, .. save, .. closure, .. overwrite, .. reload,
            ExternalCall, Zero, Zero, .. finish
        ];
        byte[] tailOnly =
        [
            .. entry, .. save, .. closure, .. overwrite, .. reload,
            TailExternalCallWithFrame, Two, Zero, Two, EndCode
        ];
        byte[] clobbered =
        [
            .. entry, .. save, .. closure, .. overwrite,
            Move, X0, X2, .. reload, .. consume, .. finish
        ];
        if (Rewrite(unconsumed, removeSpills: true) is not null || Rewrite(tailOnly, removeSpills: true) is not null
            || Rewrite(clobbered, removeSpills: true) is not null)
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.UnsafeMotion);
        }
    }

    private static void CheckStraightCallArgumentConsumers()
    {
        const byte Y1 = 0x14;
        byte[] frame = [Label, One, Allocate, Two, Four];
        byte[] steady = [Move, X3, Y1];
        byte[] firstSave = [Move, X2, Y0];
        byte[] initialized = [InitYRegisters, ExtendedListOperand, One, Y0];
        byte[] shift = [Move, X0, X2];
        byte[] reload = [Move, Y0, X1];
        byte[] constant = [Move, Atom, X0];
        byte[] finish = [Move, Y1, X0, Deallocate, Two, Return, EndCode];
        byte[] regular =
        [
            .. frame, .. steady, .. firstSave, .. shift, .. reload, .. constant,
            Line, One, ExternalCall, Three, Zero, .. finish
        ];
        byte[] regularExpected =
        [
            .. frame, .. steady, .. initialized, Move, X2, X1, .. shift, .. constant,
            Line, One, ExternalCall, Three, Zero, .. finish
        ];
        byte[] fun =
        [
            .. frame, .. steady, Move, X0, Y0, Move, X3, X0,
            .. reload, CallFun, Two, .. finish
        ];
        byte[] funExpected =
        [
            .. frame, .. steady, .. initialized, Move, X0, X1, Move, X3, X0,
            CallFun, Two, .. finish
        ];
        if (Rewrite(regular, removeSpills: true) is not { } regularCopy
            || !regularCopy.AsSpan().SequenceEqual(regularExpected)
            || Rewrite(fun, removeSpills: true) is not { } funCopy
            || !funCopy.AsSpan().SequenceEqual(funExpected))
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.MissingMotion);
        }

        byte[] overwritten =
        [
            .. frame, .. steady, .. firstSave, .. shift, .. reload,
            Move, Atom, X1, ExternalCall, Three, Zero, .. finish
        ];
        byte[] implicitFunOperand =
        [
            .. frame, .. steady, Move, X0, Y0, Move, X3, X0,
            Move, Y0, X2, CallFun, Two, .. finish
        ];
        if (Rewrite(overwritten, removeSpills: true) is not null
            || Rewrite(implicitFunOperand, removeSpills: true) is not null)
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.UnsafeMotion);
        }
    }

    private static void CheckSendArgumentMotion()
    {
        byte[] frame = [Label, One, Allocate, One, Four];
        byte[] save = [Move, X0, Y0];
        byte[] shift = [Move, X3, X0];
        byte[] reload = [Move, Y0, X1];
        byte[] finish = [Send, Deallocate, One, Return, EndCode];
        byte[] send = [.. frame, .. save, .. shift, .. reload, .. finish];
        byte[] expected =
        [
            .. frame, InitYRegisters, ExtendedListOperand, One, Y0,
            Move, X0, X1, .. shift, .. finish
        ];

        if (Rewrite(send, removeSpills: true) is not { } moved || !moved.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.MissingMotion);
        }

        byte[] outsideArguments = [.. frame, .. save, .. shift, Move, Y0, X2, .. finish];
        byte[] collecting = [.. frame, .. save, .. shift, .. reload, TestHeap, One, Two, .. finish];
        byte[] tail =
        [
            .. frame, .. save, .. shift, .. reload,
            TailExternalCallWithFrame, Two, Zero, One, EndCode
        ];
        if (Rewrite(outsideArguments, removeSpills: true) is not null
            || Rewrite(collecting, removeSpills: true) is not null
            || Rewrite(tail, removeSpills: true) is not null)
        {
            throw new InvalidOperationException(BeamEntryArgumentMotionDiagnostics.UnsafeMotion);
        }
    }

    private static byte[]? Rewrite(byte[] source, bool removeSpills = false)
    {
        var slots = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordSlot = (start, end, slot) => slots.Add((start, end, slot));
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
            recordSlot
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "EliminateEntryArgumentCopies",
            instructions,
            body,
            slots,
            removeSpills,
            false
        );
    }
}
