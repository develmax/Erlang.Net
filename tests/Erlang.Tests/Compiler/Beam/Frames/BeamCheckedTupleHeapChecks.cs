using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedTupleHeapChecks
{
    internal const string TestName = "compiler/beam/hoists-heap-before-checked-tuple-reads-with-proven-roots";
    private const string ExpectedSchedule = "Expected the independent guard, the tuple-rooted heap check, then the original field reads.";
    private const string UnsafeSchedule = "A failure read, missing root prefix, field-dependent guard, call, or collector must block the heap move.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte X2 = 0x23;
    private const byte X3 = 0x33;
    private const byte Zero = 0;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Failed = 0x25;
    private const byte Tag = 0x12;
    private const byte Fallback = 0x22;

    internal static Task Run()
    {
        byte[] checkedTuple = [IsTaggedTuple, Failed, X0, Three, Tag];
        byte[] fields =
        [
            GetTupleElement, X0, Zero, X1,
            GetTupleElement, X0, One, X3,
            GetTupleElement, X0, Two, X0
        ];
        byte[] guard = [IsNil, Failed, X2];
        byte[] heap = [TestHeap, Four, Four];
        byte[] result = [PutTuple, X0, ExtendedListOperand, Three, X0, X1, X3, Return];
        byte[] failure = [Label, Two, Move, Fallback, X0, Return, EndCode];
        byte[] source = [.. checkedTuple, .. fields, .. guard, .. heap, .. result, .. failure];
        byte[] expected = [.. checkedTuple, .. guard, TestHeap, Four, One, .. fields, .. result, .. failure];
        if (Rewrite(source) is not { } optimized || !optimized.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(ExpectedSchedule);
        }

        byte[] readsOnFailure = [.. checkedTuple, .. fields, .. guard, .. heap, .. result, Label, Two, Return, EndCode];
        byte[] readsOldRoot = [
            .. checkedTuple, .. fields, .. guard, .. heap,
            PutTuple, X0, ExtendedListOperand, Four, X0, X1, X2, X3, Return, .. failure
        ];
        byte[] readsProjection = [.. checkedTuple, .. fields, IsNil, Failed, X1, .. heap, .. result, .. failure];
        byte[] interveningCall = [.. checkedTuple, ExternalCall, One, Zero, .. fields, .. guard, .. heap, .. result, .. failure];
        byte[] interveningHeap = [.. checkedTuple, TestHeap, Zero, One, .. fields, .. guard, .. heap, .. result, .. failure];
        if (Rewrite(readsOnFailure) is not null || Rewrite(readsOldRoot) is not null
            || Rewrite(readsProjection) is not null || Rewrite(interveningCall) is not null
            || Rewrite(interveningHeap) is not null)
        {
            throw new InvalidOperationException(UnsafeSchedule);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] code)
    {
        ReadOnlyMemory<byte> instructions = code;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)Swap,
            3,
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
            "MoveHeapTestsBeforeNeutralInstructions",
            instructions,
            body,
            Array.Empty<string>(),
            Array.Empty<(int Module, int Function, int Arity)>()
        );
    }
}
