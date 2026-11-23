using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConsumedTaggedCallArgumentsChecks
{
    internal const string TestName = "compiler/beam/clears-dead-y-arguments-after-their-last-stack-reads";
    private const string WrongLayout = "The call result, its checked path, or its stack arguments were scheduled incorrectly.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Y0 = 0x04;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte AtomOne = 0x12;
    private const byte AtomTwo = 0x22;
    private const byte Failure = 0x35;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte Six = 0x60;
    private const byte IntegerOne = 0x11;
    private const byte Fallback = 0x45;
    private const byte Accepted = 0x55;
    private const byte Declined = 0x65;

    internal static Task Run()
    {
        byte[] source = Image(cleared: false, readAfterCall: false);
        byte[] expected = Image(cleared: true, readAfterCall: false);
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(Image(cleared: false, readAfterCall: true)) is not null
            || Rewrite(expected) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] fallback = FallbackImage(cleared: false, readAfterCall: false);
        byte[] expectedFallback = FallbackImage(cleared: true, readAfterCall: false);
        if (Rewrite(fallback, labelCount: 7) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expectedFallback)
            || Rewrite(FallbackImage(cleared: false, readAfterCall: true), labelCount: 7) is not null
            || Rewrite(FallbackImage(cleared: false, readAfterCall: false, extraEntry: true), labelCount: 7) is not null
            || Rewrite(FallbackImage(cleared: false, readAfterCall: false, otherCarrier: true), labelCount: 7) is not null
            || Rewrite(expectedFallback, labelCount: 7) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(bool cleared, bool readAfterCall)
    {
        byte[] kills = cleared ? [InitYRegisters, ExtendedListOperand, Two, Y1, Y2] : [];
        byte[] continuation = readAfterCall ? [Move, Y2, X0] : [Move, Y0, X0];

        return
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, Three, Label, Two,
            Allocate, Three, Three, InitYRegisters, ExtendedListOperand, One, Y0,
            Move, X1, Y1, Move, X2, Y2,
            ExternalCall, One, Zero, IsTaggedTuple, Failure, X0, Two, AtomTwo,
            GetTupleElement, X0, One, Y0, Move, Y1, X2, Move, Y2, X1,
            .. kills, Move, Y0, X0, Line, One, ExternalCall, Three, Zero,
            .. continuation, Deallocate, Three, Return,
            Label, Three, Deallocate, Three, Return, EndCode
        ];
    }

    private static byte[] FallbackImage(
        bool cleared,
        bool readAfterCall,
        bool extraEntry = false,
        bool otherCarrier = false
    )
    {
        byte[] kills = cleared ? [InitYRegisters, ExtendedListOperand, One, Y0] : [];
        byte[] accepted = extraEntry ? [Jump, Fallback] : [Move, Y1, X0, Deallocate, Three, Return];
        byte[] continuation = readAfterCall ? [Move, Y0, X0] : [];

        return
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, Three, Label, Two, Allocate, Three, Three,
            Move, X0, Y0, Move, X1, Y1, Move, X2, Y2,
            IsTaggedTuple, Fallback, otherCarrier ? Y1 : Y0, Two, AtomTwo,
            SelectValue, Y2, Fallback, ExtendedListOperand, Four, AtomOne, Accepted, AtomTwo, Declined,
            Label, Five, .. accepted, Label, Six, Move, Y2, X0, Deallocate, Three, Return,
            Label, Four, Move, IntegerOne, X1, Move, Y0, X2, .. kills, Move, Y1, X0,
            Line, One, ExternalCall, Three, Zero, .. continuation, Deallocate, Three, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code, int labelCount = 4)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            labelCount,
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
            "RewriteConsumedTaggedCallArguments",
            image,
            body
        );
    }
}
