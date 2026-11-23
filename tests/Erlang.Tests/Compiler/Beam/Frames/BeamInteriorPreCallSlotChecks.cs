using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInteriorPreCallSlotChecks
{
    internal const string TestName = "compiler/beam/promotes-interior-projection-before-first-call";
    private const string MissingProjection = "A lower projected slot should not need a frame lifetime when only the higher slot crosses the first call.";
    private const string InvalidProjection = "A slot used after the call or held through heap collection must stay on the stack.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Failure = 0x35;

    internal static Task Run()
    {
        byte[] prefix = [Allocate, Two, Two, InitYRegisters, ExtendedListOperand, One, Y0, Move, X1, Y1];
        byte[] fields = [GetTupleElement, X0, One, Y0, IsNil, Failure, Y0, Move, Y0, X0];
        byte[] call = [Label, Three, ExternalCall, One, Zero];
        byte[] tail = [TestHeap, Three, One, PutTuple, X0, ExtendedListOperand, Two, Y1, X0, Deallocate, Two, Return, EndCode];
        byte[] source = [.. prefix, .. fields, .. call, .. tail];
        byte[] expected =
        [
            .. prefix,
            GetTupleElement, X0, One, X1,
            IsNil, Failure, X1,
            Move, X1, X0,
            .. call,
            .. tail
        ];
        if (Rewrite(source) is not { } optimized || !optimized.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingProjection);
        }

        byte[] retained =
        [
            .. prefix, .. fields, .. call,
            TestHeap, Four, One,
            PutTuple, X0, ExtendedListOperand, Three, Y1, Y0, X0,
            Deallocate, Two, Return, EndCode
        ];
        byte[] collected =
        [
            .. prefix,
            GetTupleElement, X0, One, Y0,
            TestHeap, Zero, One,
            IsNil, Failure, Y0,
            Move, Y0, X0,
            .. call, .. tail
        ];
        if (Rewrite(retained) is not null || Rewrite(collected) is not null)
        {
            throw new InvalidOperationException(InvalidProjection);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        var slots = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordSlot = (start, end, slot) => slots.Add((start, end, slot));
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
            1,
            0,
            null,
            recordSlot
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "PromoteTransientFrameSlots",
            instructions,
            body,
            slots
        );
    }
}
