using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConsumedTupleCarrierChecks
{
    internal const string TestName = "compiler/beam/reuses-a-consumed-tuple-carrier-through-a-calling-continuation";
    private const string WrongLayout = "Reusing the carrier changed a call input, a checked failure, or the returned value.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte AtomOne = 0x12;
    private const byte AtomTwo = 0x22;
    private const byte ProducerFailure = 0x35;
    private const byte ActionFailure = 0x45;

    internal static Task Run()
    {
        byte[] source = Image(reordered: false);
        byte[] expected = Image(reordered: true);
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(Image(reordered: false, liveScratch: true)) is not null
            || Rewrite(Image(reordered: false, sharedFailure: true)) is not null
            || Rewrite(Image(reordered: false, wrongCleanupValue: true)) is not null
            || Rewrite(expected) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        // The generic status capture also admits this input. It must
        // follow the specialized owner exchange or consume its cold save.
        byte[]? early = Rewrite(source, "RewriteReturnedStatusCapture");
        if (early is null || Rewrite(early) is not null
            || Rewrite(actual, "RewriteReturnedStatusCapture") is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(
        bool reordered,
        bool liveScratch = false,
        bool sharedFailure = false,
        bool wrongCleanupValue = false
    )
    {
        byte[] otherRoot = liveScratch ? [Move, X0, Y0] : [];
        byte[] arguments = reordered
            ? [GetTupleElement, Y2, Three, X1, GetTupleElement, X0, One, Y2, Move, Y2, X0]
            : [GetTupleElement, X0, One, Y0, GetTupleElement, Y2, Three, X1, Move, Y0, X0];
        byte[] status = reordered ? [Move, X0, Y0] : [];
        byte[] close = reordered
            ? [Move, Y2, X0, Move, Y0, Y2]
            : [Move, X0, Y2, Move, wrongCleanupValue ? Y1 : Y0, X0];

        return
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, One, Label, Two,
            Allocate, Three, One, InitYRegisters, ExtendedListOperand, Three, Y0, Y1, Y2,
            Move, X0, Y2, .. otherRoot,
            ExternalCall, One, Zero, Move, X0, Y1,
            IsTaggedTuple, sharedFailure ? ActionFailure : ProducerFailure, X0, Two, AtomTwo,
            .. arguments, Line, One, ExternalCall, Two, Zero, .. status,
            ExactEqualTest, ActionFailure, X0, AtomOne,
            Move, Y1, X0, Deallocate, Three, Return,
            Label, Four, .. close, TrimFrame, Two, One,
            Line, Two, ExternalCall, One, Zero, Move, Y0, X0, Deallocate, One, Return,
            Label, Three, Deallocate, Three, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code, string method = "RewriteConsumedTupleCarrier")
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            5,
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
            "RewriteConsumedTupleCarrier",
            image,
            body
        );
    }
}
