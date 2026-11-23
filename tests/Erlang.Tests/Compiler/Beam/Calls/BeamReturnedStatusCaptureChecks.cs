using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamReturnedStatusCaptureChecks
{
    internal const string TestName = "compiler/beam/captures-a-cold-call-status-before-the-predicate";
    private const string WrongLayout = "The cold continuation changed its call input, status, or frame.";
    private const byte X0 = 0x03;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte AtomOne = 0x12;
    private const byte AtomTwo = 0x22;
    private const byte Failure = 0x35;

    internal static Task Run()
    {
        byte[] source = Image(ordered: false);
        byte[] expected = Image(ordered: true);
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(Image(ordered: false, sharedFailure: true)) is not null
            || Rewrite(Image(ordered: false, successUsesSlot: true)) is not null
            || Rewrite(Image(ordered: false, cleanupUsesSlot: true)) is not null
            || Rewrite(expected) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(
        bool ordered,
        bool sharedFailure = false,
        bool successUsesSlot = false,
        bool cleanupUsesSlot = false
    )
    {
        byte[] secondEdge = sharedFailure ? [IsTuple, Failure, X0] : [];
        byte[] save = [Move, X0, Y2];
        byte[] before = ordered ? save : [];
        byte[] after = ordered ? [] : save;

        return
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, One, Label, Two,
            Allocate, Three, One, InitYRegisters, ExtendedListOperand, Three, Y0, Y1, Y2,
            Move, X0, Y0, Move, X0, Y1, .. secondEdge,
            Line, One, ExternalCall, One, Zero,
            .. before, ExactEqualTest, Failure, X0, AtomOne,
            Move, successUsesSlot ? Y2 : Y1, X0, Deallocate, Three, Return,
            Label, Three, .. after, Move, cleanupUsesSlot ? Y2 : Y0, X0, TrimFrame, Two, One,
            Line, Two, ExternalCall, One, Zero, Move, Y0, X0, Deallocate, One, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            image
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
            "RewriteReturnedStatusCapture",
            image,
            body
        );
    }
}
