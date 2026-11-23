using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDisjointTaggedEntryFrameChecks
{
    internal const string TestName = "compiler/beam/splits-disjoint-tagged-entry-call-frames";
    private const string MissingSplit = "Each disjoint calling arm must restore the original tagged-entry frame.";
    private const string InvalidSplit = "Joined calling arms and overwritten saved arguments must keep their entry frame.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Tag = 0x32;
    private const byte Rejected = 0x35;
    private const byte Joined = 0x45;
    private const byte MatchedCall = 0x55;
    private const byte RejectedCall = 0x65;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] setup = [Allocate, Two, Two, Move, X1, Y0, InitYRegisters, ExtendedListOperand, One, Y1];
        byte[] check = [IsTaggedTuple, Rejected, Y0, Two, Tag];
        byte[] precheck = [IsTaggedTuple, Rejected, X1, Two, Tag];
        byte[] matched = [Line, One, Move, Y0, X0, LocalCall, One, MatchedCall];
        byte[] rejected = [Line, Two, Move, Y0, X0, LocalCall, One, RejectedCall];
        byte[] finish = [Deallocate, Two, Return];

        byte[] disjoint =
        [
            .. entry, .. setup, .. check, .. matched, .. finish,
            Label, Three, .. rejected, .. finish, EndCode
        ];
        byte[] expected =
        [
            .. entry, .. precheck, .. setup, .. matched, .. finish,
            Label, Three, .. setup, .. rejected, .. finish, EndCode
        ];
        if (Rewrite(disjoint) is not { } split || !split.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingSplit);
        }

        byte[] joined =
        [
            .. entry, .. setup, .. check, .. matched, Jump, Joined,
            Label, Three, .. rejected, Label, Four, .. finish, EndCode
        ];
        byte[] overwritten =
        [
            .. entry, .. setup, InitYRegisters, ExtendedListOperand, One, Y0,
            .. check, .. matched, .. finish,
            Label, Three, .. rejected, .. finish, EndCode
        ];
        if (Rewrite(joined) is not null || Rewrite(overwritten) is not null)
        {
            throw new InvalidOperationException(InvalidSplit);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            7,
            1,
            instructions
        );
        var stackSlots = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordSlot = (start, end, slot) => stackSlots.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            null,
            recordSlot
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "DelayEntryStackFrame",
            instructions,
            body,
            stackSlots,
            null,
            new Func<object, bool>(static _ => false),
            false
        );
    }
}
