using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedCallPayloadCarrierChecks
{
    internal const string TestName = "compiler/beam/keeps-a-dead-guarded-call-payload-in-x";
    private const string WrongLayout = "The guarded carrier moved through a live call or changed its error routes.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte Six = 0x60;
    private const byte Seven = 0x70;
    private const byte Eight = 0x80;
    private const byte Nine = 0x90;
    private const byte AtomOne = 0x12;
    private const byte AtomTwo = 0x22;
    private const byte IntegerZero = 0x01;
    private const byte Hot = 0x35;
    private const byte Fall = 0x45;
    private const byte Join = 0x55;
    private const byte Accept = 0x65;
    private const byte ErrorTag = 0x75;
    private const byte Reject = 0x85;
    private const byte Default = 0x95;

    internal static Task Run()
    {
        byte[] source = MakeImage(Y1, X2);
        byte[] expected = MakeImage(X2, X3);
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Rewrite(MakeImage(Y1, X2, readAfterCall: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object promoted = Decode(actual);
        if (Call(
            typeof(BeamModuleCompiler),
            "RewriteProjectedCallArgumentSwap",
            new ReadOnlyMemory<byte>(actual),
            promoted
        ) is not byte[])
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] MakeImage(byte carrier, byte scratch, bool readAfterCall = false)
    {
        byte[] prefix =
        [
            Label, One, FunctionInfo, AtomOne, AtomTwo, One, Label, Two,
            Allocate, Two, One, InitYRegisters, ExtendedListOperand, Two, Y0, Y1,
            ExternalCall, One, Zero,
            IsTuple, Default, X0, TestArity, Default, X0, Two,
            GetTupleElement, X0, Zero, X1, GetTupleElement, X0, One, carrier,
            SelectValue, X1, Default, ExtendedListOperand, Four, AtomOne, Hot, AtomTwo, ErrorTag,
            Label, Three, IsTaggedTuple, Reject, carrier, Four, AtomTwo,
            GetTupleElement, carrier, Two, X0, ExactEqualTest, Reject, X0, IntegerZero,
            GetTupleElement, carrier, One, Y0, Move, Y0, X0
        ];
        byte[] guard =
        [
            BifOne, Fall, Zero, X0, X1,
            BifTwo, Zero, Zero, X1, IntegerZero, X1,
            BifTwo, Fall, Zero, IntegerZero, X0, scratch,
            BifTwo, Zero, Zero, scratch, AtomOne, scratch,
            BifTwo, Zero, Zero, X1, ExtendedTypedRegisterOperand, scratch, Zero, X1,
            Jump, Join, Label, Four, Move, AtomTwo, X1,
            Label, Five, BifTwo, Zero, Zero, X0, AtomTwo, scratch,
            ExactEqualTest, Accept, X1, AtomTwo, ExactEqualTest, Reject, scratch, AtomOne, Label, Six
        ];
        byte[] call =
        [
            GetTupleElement, carrier, Two, X0,
            GetTupleElement, carrier, Three, X3,
            Move, Y0, X1, Move, IntegerZero, X2,
            Line, One, ExternalCall, Four, Zero
        ];
        byte[] continuation = readAfterCall ? [Move, Y1, X0] : [];

        return
        [
            .. prefix, .. guard, .. call, .. continuation, Deallocate, Two, Return,
            Label, Eight, Move, AtomTwo, X0, TailExternalCallWithFrame, One, Zero, Two,
            Label, Seven, Move, carrier, X0, TailExternalCallWithFrame, One, Zero, Two,
            Label, Nine, CaseEnd, X0, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code) => (byte[]?)Call(
        typeof(BeamModuleCompiler),
        "RewriteGuardedCallPayloadCarrier",
        new ReadOnlyMemory<byte>(code),
        Decode(code)
    );

    private static object Decode(byte[] code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            10,
            1,
            new ReadOnlyMemory<byte>(code)
        );

        return Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );
    }
}
