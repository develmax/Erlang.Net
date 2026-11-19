using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedTagZeroFrameFallbackChecks
{
    internal const string TestName = "compiler/beam/delays-a-zero-slot-frame-after-a-checked-tag-fallback";
    private const string MissingPlacement = "Expected the checked local tail to use no frame and the fallback to retain its own allocation and line.";
    private const string InvalidPlacement = "A positive-width frame, modified selector, intervening call, handler, or cross-entry must keep its original placement.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Nil = 0x02;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Tag = 0x32;
    private const byte Entry = 0x25;
    private const byte TestTag = 0x35;
    private const byte Success = 0x45;
    private const byte Fallback = 0x55;
    private const byte Inspect = 0x65;
    private const byte Finish = 0x75;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, One, Label, Two];
        byte[] frame = [Allocate, Zero, One];
        byte[] tag = [IsTaggedTuple, Fallback, X0, Three, Tag];
        byte[] alternative = [IsPid, TestTag, X0, Jump, Success, Label, Three];
        byte[] successful = [Label, Four, Move, Nil, X1];
        byte[] rejected = [Line, One, LocalCall, One, Inspect, TailLocalCallWithFrame, One, Finish, Zero, EndCode];
        byte[] selection = [TailLocalCallWithFrame, One, Finish, Zero, Label, Five];
        byte[] selectionWithOption = [TailLocalCallWithFrame, Two, Finish, Zero, Label, Five];

        byte[] direct = [.. entry, .. frame, .. tag, .. selection, .. rejected];
        byte[] directExpected =
        [
            .. entry, .. tag, TailLocalCall, One, Finish, Label, Five, .. frame, .. rejected
        ];
        byte[] disjunction =
        [
            .. entry, .. frame, .. alternative, .. tag, .. successful, .. selectionWithOption, .. rejected
        ];
        byte[] disjunctionExpected =
        [
            .. entry, .. alternative, .. tag, .. successful, TailLocalCall, Two, Finish, Label, Five, .. frame, .. rejected
        ];
        if (Rewrite(direct) is not { } directActual || !directActual.AsSpan().SequenceEqual(directExpected)
            || Rewrite(disjunction) is not { } disjunctionActual || !disjunctionActual.AsSpan().SequenceEqual(disjunctionExpected))
        {
            throw new InvalidOperationException(MissingPlacement);
        }

        byte[] positiveWidth = [.. entry, Allocate, One, One, .. tag, .. selection, .. rejected];
        byte[] writesSelector = [.. entry, .. frame, .. tag, Move, Nil, X0, .. selection, .. rejected];
        byte[] priorCall = [.. entry, .. frame, LocalCall, One, Inspect, .. tag, .. selection, .. rejected];
        byte[] handler =
        [
            .. entry, .. frame, .. tag, .. selection,
            LocalCall, One, Inspect, CatchEnter, Y0, Fallback, TailLocalCallWithFrame, One, Finish, Zero, EndCode
        ];
        byte[] crossesEntry =
        [
            .. entry, .. frame, .. tag, .. selection, LocalCall, One, Inspect, Jump, Entry, EndCode
        ];
        byte[] sharedFallback =
        [
            .. entry, .. frame, .. tag, .. selection, LocalCall, One, Inspect, Jump, Fallback, EndCode
        ];
        if (Plan(positiveWidth) is not null || Plan(writesSelector) is not null || Plan(priorCall) is not null
            || Plan(handler) is not null || Plan(crossesEntry) is not null || Plan(sharedFallback) is not null)
        {
            throw new InvalidOperationException(InvalidPlacement);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayEntryStackFrame",
            instructions,
            Read(instructions),
            new List<(int Start, int End, int Slot)>(),
            null
        );
    }

    private static object? Plan(byte[] source)
    {
        object body = Read(source);
        int count = (int)Property(body, "Count");

        return Call(
            typeof(BeamModuleCompiler),
            "PlanCheckedTagFallbackFrame",
            body,
            2,
            3,
            count - 1
        );
    }

    private static object Read(ReadOnlyMemory<byte> instructions)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            8,
            1,
            instructions
        );

        return Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            null,
            null
        );
    }
}
