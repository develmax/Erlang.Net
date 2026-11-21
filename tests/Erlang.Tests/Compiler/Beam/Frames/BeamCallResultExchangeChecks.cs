using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCallResultExchangeChecks
{
    internal const string TestName = "compiler/beam/exchanges-call-result-and-saved-input-in-one-frame-cell";
    private const string MissingExchange = "Expected a single saved slot and a swap with the result register.";
    private const string UnsafeExchange = "Expected the frame to retain slots across an intervening collector, branch or later read.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Entry = 0x25;
    private const byte Branch = 0x35;
    private const byte Module = 0x12;
    private const byte Function = 0x22;

    internal static Task Run()
    {
        byte[] header = [Label, One, FunctionInfo, Module, Function, Two, Label, Two];
        byte[] tail = [Line, Two, LocalCall, Two, Entry, Deallocate, Two, Return, EndCode];
        byte[] expected =
        [
            .. header, Allocate, One, Two, Move, X1, Y0, LocalCall, One, Entry,
            Swap, Y0, X0, LocalCall, One, Entry, Move, X0, X1, Move, Y0, X0,
            InitYRegisters, ExtendedListOperand, One, Y0,
            Line, Two, LocalCall, Two, Entry, Deallocate, One, Return, EndCode
        ];

        foreach (var (old, saved) in new[] { (Y0, Y1), (Y1, Y0) })
        {
            byte[] frame =
            [
                Allocate, Two, Two, InitYRegisters, ExtendedListOperand, One, saved,
                Move, X1, old, LocalCall, One, Entry
            ];
            byte[] copies = [Move, X0, saved, Move, old, X0];
            byte[] consume = [LocalCall, One, Entry, Move, X0, X1, Move, saved, X0];
            byte[] original = [.. header, .. frame, .. copies, .. consume, .. tail];
            byte[] withDeadSpill =
            [
                .. header, .. frame, .. copies, .. consume,
                Line, Two, LocalCall, Two, Entry, Move, X0, saved, Deallocate, Two, Return, EndCode
            ];
            if (Rewrite(original) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
                || Rewrite(withDeadSpill) is not { } actualSpill || !actualSpill.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidOperationException(MissingExchange);
            }

            byte[] collecting =
            [
                .. header, .. frame, .. copies, TestHeap, One, Two, .. consume, .. tail
            ];
            byte[] merging =
            [
                .. header, .. frame, .. copies, IsAtom, Branch, X0, Label, Three, .. consume, .. tail
            ];
            byte[] laterRead =
            [
                .. header, .. frame, .. copies, .. consume,
                Line, Two, LocalCall, Two, Entry, Move, saved, X0, Deallocate, Two, Return, EndCode
            ];
            if (Rewrite(collecting) is not null || Rewrite(merging) is not null || Rewrite(laterRead) is not null)
            {
                throw new InvalidOperationException(UnsafeExchange);
            }
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
            4,
            1,
            instructions
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "ExchangeCallResultWithSavedInput",
            instructions,
            body
        );
    }
}
