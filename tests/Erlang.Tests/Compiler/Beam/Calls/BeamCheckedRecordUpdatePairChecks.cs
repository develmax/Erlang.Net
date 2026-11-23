using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedRecordUpdatePairChecks
{
    internal const string TestName = "compiler/beam/forwards-checked-call-pair-across-record-update";
    private const string WrongSchedule = "The checked record update changed the payload, failure path, or heap roots.";
    private const string RewroteControl = "The checked record update crossed an uninitialized slot or live argument.";
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
    private const byte Failure = 0x25;
    private const byte Alternate = 0x35;
    private const byte Tag = 0x12;

    internal static Task Run()
    {
        byte[] frame = [Label, One, Allocate, Three, Four, Move, X1, Y1];
        byte[] initialized = [InitYRegisters, ExtendedListOperand, Two, Y0, Y2];
        byte[] guards =
        [
            ExternalCall, Two, Zero,
            IsTuple, Alternate, X0,
            IsTaggedTuple, Failure, X0, Two, Tag,
            GetTupleElement, X0, One, X1,
            IsTuple, Failure, X1,
            TestArity, Failure, X1, Two
        ];
        byte[] previous =
        [
            GetTupleElement, X1, Zero, Y2,
            GetTupleElement, X1, One, Y0,
            TestHeap, Four, Zero,
            UpdateRecord, Tag, Three, ExtendedTypedRegisterOperand, Y1, One, X1, ExtendedListOperand, Two, One, Y0,
            Move, Y2, X0
        ];
        byte[] replacement =
        [
            TestHeap, Four, Two,
            GetTupleElement, X1, One, X0,
            UpdateRecord, Tag, Three, ExtendedTypedRegisterOperand, Y1, One, X0, ExtendedListOperand, Two, One, X0,
            GetTupleElement, X1, Zero, X1,
            Swap, X0, X1
        ];
        byte[] killed = [InitYRegisters, ExtendedListOperand, Three, Y0, Y1, Y2];
        byte[] call = [ExternalCall, Two, Zero];
        byte[] suffix =
        [
            Deallocate, Three, Return,
            Label, Three, Move, Y1, X0, Deallocate, Three, Return,
            Label, Two, CaseEnd, X0, EndCode
        ];
        byte[] source = [.. frame, .. initialized, .. guards, .. previous, .. killed, .. call, .. suffix];
        byte[] expected = [.. frame, .. initialized, .. guards, .. replacement, .. killed, .. call, .. suffix];
        if (Rewrite(source) is not { } rewritten || !rewritten.SequenceEqual(expected))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        byte[] alreadyRooted = [.. previous];
        alreadyRooted[10] = One;
        if (Rewrite([.. frame, .. guards, .. previous, .. killed, .. call, .. suffix]) is not null
            || Rewrite([.. frame, .. initialized, .. guards, .. alreadyRooted, .. killed, .. call, .. suffix]) is not null
            || Rewrite([.. frame, .. initialized, .. guards, .. previous, .. killed, ExternalCall, Three, Zero, .. suffix]) is not null)
        {
            throw new InvalidOperationException(RewroteControl);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        var code = new ReadOnlyMemory<byte>(source);
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            4,
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
            "RewriteCheckedRecordUpdatePair",
            code,
            instructions
        );
    }
}
