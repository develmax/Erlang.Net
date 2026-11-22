using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedRecordPairFrameChecks
{
    internal const string TestName = "compiler/beam/compacts-locally-owned-checked-record-pair-frame";
    private const string WrongSchedule = "The checked record pair changed a leaf's frame or retained record.";
    private const string RewroteControl = "The checked record pair compacted a frame with an incoming edge or an extra root.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X3 = 0x33;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Alternate = 0x25;
    private const byte Invalid = 0x35;
    private const byte Outside = 0x45;
    private const byte Tag = 0x12;

    internal static Task Run()
    {
        byte[] entry = [Label, One, IsTaggedTuple, Outside, X3, Four, Tag];
        byte[] beforeCall =
        [
            Allocate, Three, Four, Move, X3, Y1, GetTupleElement, X3, One, X0, Swap, X0, X1,
            InitYRegisters, ExtendedListOperand, Two, Y0, Y2
        ];
        byte[] checks =
        [
            Line, One, ExternalCall, Two, Zero, IsTuple, Alternate, X0, IsTaggedTuple, Invalid, X0, Two, Tag,
            GetTupleElement, X0, One, X1, IsTuple, Invalid, X1, TestArity, Invalid, X1, Two,
            TestHeap, Four, Two, GetTupleElement, X1, One, X0, UpdateRecord, Tag, Four,
            ExtendedTypedRegisterOperand, Y1, One, X0, ExtendedListOperand, Two, One, X0,
            GetTupleElement, X1, Zero, X1, Swap, X0, X1
        ];
        byte[] hotPrefix =
        [
            InitYRegisters, ExtendedListOperand, Three, Y0, Y1, Y2,
            Line, Two, ExternalCall, Two, Zero, TestHeap, Four, One
        ];
        byte[] hotReturn = [PutTuple, X0, ExtendedListOperand, Two, Tag, X0, Deallocate, Three, Return];
        byte[] coldReturn =
        [
            Label, Two, ExactEqualTest, Invalid, X0, Tag, TestHeap, Four, Zero,
            PutTuple, X0, ExtendedListOperand, Two, Tag, Y1, Deallocate, Three, Return
        ];
        byte[] tail = [Label, Three, Line, One, CaseEnd, X0, Label, Four, Return, EndCode];
        byte[] source = [.. entry, .. beforeCall, .. checks, .. hotPrefix, .. hotReturn, .. coldReturn, .. tail];
        byte[] compactChecks = [.. checks];
        compactChecks[35] = Y0;
        byte[] compactCold = [.. coldReturn];
        compactCold[14] = Y0;
        compactCold[16] = One;
        byte[] expected =
        [
            .. entry,
            Allocate, One, Four, Move, X3, Y0, GetTupleElement, X3, One, X0, Swap, X0, X1,
            .. compactChecks,
            TrimFrame, One, Zero, Line, Two, ExternalCall, Two, Zero, TestHeap, Four, One,
            PutTuple, X0, ExtendedListOperand, Two, Tag, X0, Deallocate, Zero, Return,
            .. compactCold,
            .. tail
        ];
        if (Rewrite(source) is not { } rewritten || !rewritten.SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        byte[] extraRoot = [.. hotReturn];
        extraRoot[5] = Y0;
        byte[] incoming =
        [
            Label, Three, Line, One, CaseEnd, X0, Label, Four,
            Allocate, Three, Four, InitYRegisters, ExtendedListOperand, Three, Y0, Y1, Y2, Jump, Alternate, EndCode
        ];
        if (Rewrite([.. entry, .. beforeCall, .. checks, .. hotPrefix, .. extraRoot, .. coldReturn, .. tail]) is not null
            || Rewrite([.. entry, .. beforeCall, .. checks, .. hotPrefix, .. hotReturn, .. coldReturn, .. incoming]) is not null)
        {
            throw new InvalidOperationException(RewroteControl);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        var memory = new ReadOnlyMemory<byte>(source);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            5,
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
            "RewriteCheckedRecordPairFrame",
            memory,
            instructions,
            offsets
        );
    }
}
