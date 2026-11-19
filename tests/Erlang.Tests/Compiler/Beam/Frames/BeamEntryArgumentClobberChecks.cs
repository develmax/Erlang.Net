using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryArgumentClobberChecks
{
    internal const string TestName = "compiler/beam/sinks-an-independent-argument-clobber-past-a-reload";
    private const string MissingMotion = "The independent source clobber must follow the direct argument copy.";
    private const string UnsafeMotion = "An intervening read, changed source or live stack value must prevent copy motion.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte Target = 0x35;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte X4 = 0x43;
    private const byte X5 = 0x53;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;

    internal static Task Run()
    {
        byte[] entry = [Label, One, Allocate, Two, Five];
        byte[] spills = [Move, X2, Y0, Move, X1, Y1];
        byte[] clobber = [Move, X4, X2];
        byte[] preserveTarget = [Move, X3, X1, Move, X0, X3];
        byte[] reload = [Move, Y0, X0];
        byte[] call = [Line, One, LocalCall, Four, Target];
        byte[] finish = [Move, Y1, X0, Deallocate, Two, Return, EndCode];
        byte[] candidate = [.. entry, .. spills, .. clobber, .. preserveTarget, .. reload, .. call, .. finish];
        byte[] expected =
        [
            .. entry, InitYRegisters, ExtendedListOperand, One, Y0, Move, X1, Y1,
            .. preserveTarget, Move, X2, X0, .. clobber, .. call, .. finish
        ];
        if (Rewrite(candidate) is not { } result || !result.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingMotion);
        }

        byte[] readsClobber =
        [
            .. entry, .. spills, .. clobber, Move, X2, X5,
            .. preserveTarget, .. reload, .. call, .. finish
        ];
        byte[] changesSource =
        [
            .. entry, .. spills, .. clobber, Move, X0, X4,
            .. preserveTarget, .. reload, .. call, .. finish
        ];
        byte[] readsSlotAfterCall =
        [
            .. entry, .. spills, .. clobber, .. preserveTarget, .. reload, .. call,
            Move, Y0, X0, Deallocate, Two, Return, EndCode
        ];
        if (Rewrite(readsClobber) is not null || Rewrite(changesSource) is not null
            || Rewrite(readsSlotAfterCall) is not null)
        {
            throw new InvalidOperationException(UnsafeMotion);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            code
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

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "SinkEntryArgumentClobbers",
            code,
            body
        );
    }
}
