using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedCallArgumentSwapChecks
{
    internal const string TestName = "compiler/beam/swaps-checked-projection-into-two-call-arguments";
    private const string WrongSchedule = "The projected two-argument call changed its roots or swap order.";
    private const string RewroteControl = "The projected argument swap crossed a live slot or call boundary.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte X4 = 0x43;
    private const byte X5 = 0x53;
    private const byte X6 = 0x63;
    private const byte X7 = 0x73;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Y2 = 0x24;
    private const byte Zero = 0;
    private const byte NoFailure = 0x05;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Failure = 0x25;
    private const byte ApplyMember = 0x12;
    private const byte BifFailure = 0x35;
    private const byte GuardJoin = 0x45;
    private const byte GuardAccept = 0x55;
    private const byte GuardReject = 0x65;
    private const byte Five = 0x50;
    private const byte Six = 0x60;
    private const byte Eight = 0x80;
    private const byte IntegerZero = 0x01;

    internal static Task Run()
    {
        byte[] prefix = [Label, One, Allocate, Three, Four];
        byte[] copies =
        [
            GetTupleElement, X3, Two, Y0,
            Move, X3, Y1,
            Move, X1, Y2,
            Move, Y0, X1,
            Move, Y2, X0
        ];
        byte[] clear = [InitYRegisters, ExtendedListOperand, Two, Y0, Y2];
        byte[] call = [ExternalCall, Two, Zero];
        byte[] suffix = [Move, Y1, X0, Deallocate, Three, Return, EndCode];
        byte[] source = [.. prefix, .. copies, .. clear, .. call, .. suffix];
        byte[] expected =
        [
            .. prefix,
            Move, X3, Y1,
            GetTupleElement, X3, Two, X0,
            Swap, X0, X1,
            .. clear,
            .. call,
            .. suffix
        ];
        if (Rewrite(source) is not { } actual || !actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        byte[] ambiguous = [.. copies];
        ambiguous[6] = Y0;
        if (Rewrite([.. prefix, .. copies, InitYRegisters, ExtendedListOperand, One, Y0, .. call, .. suffix]) is not null
            || Rewrite([.. prefix, .. ambiguous, .. clear, .. call, .. suffix]) is not null
            || Rewrite([.. prefix, .. copies, .. clear, TestHeap, Zero, Two, .. call, .. suffix]) is not null
            || Rewrite([.. prefix, .. copies, .. clear, ExternalCall, Three, Zero, .. suffix]) is not null)
        {
            throw new InvalidOperationException(RewroteControl);
        }

        CheckProjectedApply(X2);
        CheckProjectedApply(X3);
        CheckGuardedProjection(Y1);
        CheckGuardedProjection(X3);

        return Task.CompletedTask;
    }

    private static void CheckProjectedApply(byte carrier)
    {
        byte[] prefix = [Label, One, Allocate, One, Four];
        byte[] member = [Move, ApplyMember, X2];
        byte[] schedule =
        [
            Move, carrier, X1,
            IsTuple, Failure, X1,
            TestArity, Failure, X1, Three,
            GetTupleElement, X1, Zero, Y0,
            GetTupleElement, X1, One, X2,
            GetTupleElement, X1, Two, X0,
            Move, X2, X1,
            .. member
        ];
        byte[] call = [Line, One, DynamicApply, One];
        byte[] result = [Move, Y0, X0, Deallocate, One, Return, Label, Two, Line, Two];
        byte[] source = [.. prefix, .. schedule, .. call, .. result, CaseEnd, X0, EndCode];
        byte[] expected =
        [
            .. prefix,
            IsTuple, Failure, carrier,
            TestArity, Failure, carrier, Three,
            GetTupleElement, carrier, Zero, Y0,
            GetTupleElement, carrier, One, X0,
            GetTupleElement, carrier, Two, X1,
            .. member, Swap, X0, X1,
            .. call, .. result, CaseEnd, X0, EndCode
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        if (Rewrite([.. prefix, .. schedule, .. call, .. result, CaseEnd, X1, EndCode]) is not null
            || Rewrite([.. prefix, .. schedule, DynamicApply, Two, .. result, CaseEnd, X0, EndCode]) is not null)
        {
            throw new InvalidOperationException(RewroteControl);
        }
    }

    private static void CheckGuardedProjection(byte carrier)
    {
        byte[] prefix =
        [
            Label, One, Allocate, Two, Four,
            InitYRegisters, ExtendedListOperand, Two, Y0, Y1,
            Move, X3, carrier
        ];
        byte[] guard =
        [
            BifOne, BifFailure, Zero, X0, X1,
            BifTwo, NoFailure, Zero, X1, IntegerZero, X1,
            BifTwo, BifFailure, Zero, IntegerZero, X0, X2,
            BifTwo, NoFailure, Zero, X2, ApplyMember, X2,
            BifTwo, NoFailure, Zero, X1, X2, X1,
            Jump, GuardJoin,
            Label, Three, Move, ApplyMember, X1,
            Label, Four, BifTwo, NoFailure, Zero, X0, ApplyMember, X2,
            ExactEqualTest, GuardAccept, X1, ApplyMember,
            ExactEqualTest, GuardReject, X2, ApplyMember,
            Label, Five
        ];
        byte[] setup =
        [
            GetTupleElement, carrier, Two, X0,
            GetTupleElement, carrier, Three, X3,
            Move, Y0, X1,
            Move, IntegerZero, X2,
            Move, ApplyMember, X4,
            Move, ApplyMember, X5,
            Move, ApplyMember, X6,
            Move, ApplyMember, X7
        ];
        byte[] call = [Line, One, ExternalCall, Eight, Zero];
        byte[] release = [Deallocate, Two, Return, Label, Six];
        byte[] rejection = [TailExternalCallWithFrame, One, Zero, Two, EndCode];
        byte[] cold = [Move, ApplyMember, X0, .. rejection];
        byte[] source =
        [
            .. prefix, GetTupleElement, carrier, One, Y0, Move, Y0, X0,
            .. guard, .. setup, .. call, .. release, .. cold
        ];
        byte[] expected =
        [
            .. prefix, GetTupleElement, carrier, One, X0, .. guard,
            GetTupleElement, carrier, Two, X1,
            GetTupleElement, carrier, Three, X3,
            Move, ApplyMember, X5,
            Move, ApplyMember, X6,
            Move, ApplyMember, X4,
            Move, ApplyMember, X7,
            Move, IntegerZero, X2,
            Swap, X0, X1,
            .. call, .. release, .. cold
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        byte[] oldProjection = [GetTupleElement, carrier, One, Y0, Move, Y0, X0];
        byte[] readsInCold = [.. prefix, .. oldProjection, .. guard, .. setup, .. call, .. release,
            Move, Y0, X0, .. rejection];
        byte[] readsAfterCall = [.. prefix, .. oldProjection, .. guard, .. setup, .. call,
            Move, Y0, X0, .. release, .. cold];
        byte[] nonNilRoot = [.. prefix, Move, X0, Y0, .. oldProjection, .. guard, .. setup, .. call, .. release, .. cold];
        if (Rewrite(readsInCold) is not null || Rewrite(readsAfterCall) is not null || Rewrite(nonNilRoot) is not null)
        {
            throw new InvalidOperationException(RewroteControl);
        }
    }

    private static byte[]? Rewrite(byte[] source)
    {
        var code = new ReadOnlyMemory<byte>(source);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            7,
            1,
            code
        );
        object instructions = Call(
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
            "RewriteProjectedCallArgumentSwap",
            code,
            instructions
        );
    }
}
