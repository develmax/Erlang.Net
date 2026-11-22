using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedRecordUpdateRelayChecks
{
    internal const string TestName = "compiler/beam/forwards-nested-record-update-between-owned-registers";
    private const string WrongSchedule = "The nested updates lost their typed relay or final saved result.";
    private const string RewroteControl = "The nested updates rewrote an observable or unprotected relay.";
    private const byte X0 = 0x03;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Eight = 0x80;
    private const byte Tag = 0x12;

    internal static Task Run()
    {
        byte[] frame =
        [
            Label, One, Allocate, Three, Two, Move, X0, Y0, Move, X0, Y1,
            InitYRegisters, ExtendedListOperand, One, Y2
        ];
        byte[] heap = [TestHeap, Eight, Zero];
        byte[] inner =
        [
            UpdateRecord, Tag, Three, ExtendedTypedRegisterOperand, Y0, One, X0, ExtendedListOperand, Two, Two, Tag
        ];
        byte[] saved = [Move, X0, Y2];
        byte[] outer =
        [
            UpdateRecord, Tag, Three, ExtendedTypedRegisterOperand, Y1, One, X0, ExtendedListOperand, Two, Two, Y2
        ];
        byte[] suffix = [Move, X0, Y2, Move, Y2, X0, Deallocate, Three, Return, EndCode];
        byte[] source = [.. frame, .. heap, .. inner, .. saved, .. outer, .. suffix];
        byte[] expected = [.. frame, .. heap, .. inner, .. outer[..^1], ExtendedTypedRegisterOperand, X0, One, .. suffix];
        if (Rewrite(source) is not { } rewritten || !rewritten.SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        byte[] stillNeeded = [Move, Y2, X0, Deallocate, Three, Return, EndCode];
        byte[] lowHeap = [TestHeap, Three, Zero];
        if (Rewrite([.. frame, .. heap, .. inner, .. saved, .. outer, .. stillNeeded]) is not null
            || Rewrite([.. frame, .. lowHeap, .. inner, .. saved, .. outer, .. suffix]) is not null
            || Rewrite([.. frame, .. heap, .. inner, .. saved, Line, One, .. outer, .. suffix]) is not null
            || Rewrite(source, tupleType: 2) is not null)
        {
            throw new InvalidOperationException(RewroteControl);
        }

        byte[] joined = [Label, Two, Move, X0, Y2];
        byte[] tail = [TestHeap, Three, Zero, PutTuple, X0, ExtendedListOperand, Two, Tag, Y2, Deallocate, Three, Return, EndCode];
        byte[] joinedSource = [.. frame, .. heap, .. inner, .. saved, .. outer, .. joined, .. tail];
        byte[] joinedExpected =
        [
            .. frame, .. heap, .. inner, .. outer[..^1], ExtendedTypedRegisterOperand, X0, One, .. joined, .. tail
        ];
        if (Rewrite(joinedSource, joinedOnly: true) is not { } joinedRewritten
            || !joinedRewritten.SequenceEqual(joinedExpected)
            || Rewrite(joinedSource) is not null
            || Rewrite([.. frame, .. heap, .. inner, .. saved, .. outer, Label, Two, .. stillNeeded], joinedOnly: true) is not null
            || Rewrite([.. frame, .. heap, .. inner, .. saved, .. outer, .. joined, Line, One, .. tail], joinedOnly: true) is not null)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source, int tupleType = 1, bool joinedOnly = false)
    {
        var memory = new ReadOnlyMemory<byte>(source);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            3,
            1,
            memory
        );
        var offsets = new List<(int Start, int End, int Slot)>();
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            (Action<int, int, int>)((from, to, slot) => offsets.Add((from, to, slot)))
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteNestedRecordUpdateRelays",
            memory,
            instructions,
            offsets,
            tupleType,
            joinedOnly
        );
    }
}
