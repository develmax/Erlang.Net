using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalListMatchSlotChecks
{
    internal const string TestName = "compiler/beam/compacts-the-frame-after-a-terminal-list-match-relay";
    private const string MissingCompaction = "A source line between a register relay and badmatch must preserve stack slot reuse.";
    private const string InvalidCompaction = "An entry failure reading the frame or leaving its local block must prevent stack slot reuse.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Failed = 0x35;
    private const byte Other = 0x45;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;

    internal static Task Run()
    {
        byte[] entry = [Label, One, FunctionInfo, Module, Function, Two, Label, Two, IsNonemptyList, Failed, X1];
        byte[] wide =
        [
            Allocate, Two, Two,
            Move, X0, Y0, Move, Y0, X0,
            Move, X1, Y1, Move, Y1, X0,
            Deallocate, Two, Return
        ];
        byte[] narrow =
        [
            Allocate, One, Two,
            Move, X0, Y0, Move, Y0, X0,
            Move, X1, Y0, Move, Y0, X0,
            Deallocate, One, Return
        ];
        byte[] failure = [Label, Three, Move, X1, X0, Line, One, BadMatch, X0, EndCode];
        byte[] original = [.. entry, .. wide, .. failure];
        byte[] expected = [.. entry, .. narrow, .. failure];
        if (Rewrite(original) is not { } result || !result.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingCompaction);
        }

        byte[] frameRead = [.. entry, .. wide, Label, Three, Move, Y0, X0, Line, One, BadMatch, X0, EndCode];
        byte[] sideExit =
        [
            .. entry, .. wide,
            Label, Three, Move, X1, X0, Line, One, Jump, Other,
            Label, Four, BadMatch, X0, EndCode
        ];
        if (Rewrite(frameRead) is not null || Rewrite(sideExit) is not null)
        {
            throw new InvalidOperationException(InvalidCompaction);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> code = source;
        var slots = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordSlot = (start, end, slot) => slots.Add((start, end, slot));
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            5,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            recordSlot
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "ReuseStackSlots",
            code,
            body,
            slots,
            false,
            null
        );
    }
}
