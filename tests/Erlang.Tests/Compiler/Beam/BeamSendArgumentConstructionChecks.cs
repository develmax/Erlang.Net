using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSendArgumentConstructionChecks
{
    internal const string TestName = "compiler/beam/constructs-a-tuple-message-directly-in-x1";
    private const string IncorrectSchedule = "The send argument constructor should keep the original recipient and a zero-slot frame.";
    private const string UnsafeSchedule = "A retained stack value or intervening heap check must prevent this send construction rewrite.";
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte ReturnAtom = 0x12;
    private const byte MessageTag = 0x22;
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;

    internal static Task Run()
    {
        byte[] allocate = [Label, One, AllocateHeap, One, Three, Two, Move, X0, Y0];
        byte[] message = [PutTuple, X0, ExtendedListOperand, Two, MessageTag, X1];
        byte[] copies = [Move, X0, X1, Move, Y0, X0];
        byte[] finish = [Move, ReturnAtom, X0, Deallocate, One, Return, EndCode];
        byte[] source = [.. allocate, .. message, .. copies, Line, One, Send, .. finish];
        byte[] expected =
        [
            Label, One, AllocateHeap, Zero, Three, Two,
            PutTuple, X1, ExtendedListOperand, Two, MessageTag, X1,
            Line, One, Send, Move, ReturnAtom, X0, Deallocate, Zero, Return, EndCode
        ];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(IncorrectSchedule);
        }

        byte[] retained =
        [
            .. allocate, .. message, .. copies, Send, Move, Y0, X0,
            Deallocate, One, Return, EndCode
        ];
        byte[] usedInTuple =
        [
            .. allocate, PutTuple, X0, ExtendedListOperand, Two, MessageTag, Y0,
            .. copies, Send, .. finish
        ];
        byte[] collecting =
        [
            .. allocate, .. message, .. copies, TestHeap, Three, Two, Send, .. finish
        ];
        if (Rewrite(retained) is not null || Rewrite(usedInTuple) is not null || Rewrite(collecting) is not null)
        {
            throw new InvalidOperationException(UnsafeSchedule);
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
            2,
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
            "ConstructSendTupleInArgument",
            instructions,
            body
        );
    }
}
