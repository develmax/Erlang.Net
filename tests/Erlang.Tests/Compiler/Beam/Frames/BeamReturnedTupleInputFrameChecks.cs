using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamReturnedTupleInputFrameChecks
{
    internal const string TestName = "compiler/beam/roots-returned-tuple-input-without-a-stack-slot";
    private const string WrongLayout = "A returned tuple must keep X0 live while preserving the call continuation.";
    private const string UnsafeLayout = "A separately used or independently entered tuple slot must stay in its frame.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_returned_tuple_input_frame");
        if (Rewrite(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(compacted: true)))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        if (Rewrite(WiderFixture()) is not { } wider || !wider.SequenceEqual(WiderFixture(compacted: true)))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        Reject(changed);
        Reject(wider);
        Reject(WiderFixture(laterSlotRead: true));
        Reject(Fixture(laterSlotRead: true));
        Reject(Fixture(interiorEntry: true));
        Reject(Fixture(heapRooted: true));

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "KeepReturnedTupleInputInX",
            code,
            6
        );

        void Reject(byte[] code)
        {
            if (Rewrite(code) is not null)
            {
                throw new InvalidOperationException(UnsafeLayout);
            }
        }
    }

    private static byte[] WiderFixture(bool compacted = false, bool laterSlotRead = false)
    {
        int width = compacted ? 2 : 3;
        byte high = Y(compacted ? 1 : 2);
        byte input = compacted ? X(0) : Y(1);
        byte[] clear = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] spill = compacted ? [] : [Move, X(0), Y(1)];
        byte[] reload = laterSlotRead ? [Move, Y(1), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(width), U(2), .. clear,
            Move, X(0), Y(0), Move, X(1), high,
            ExternalCall, U(1), U(0),
            .. spill, TestHeap, U(4), U(compacted ? 1 : 0),
            PutTuple, X(0), ExtendedListOperand, U(3), Y(0), input, high,
            .. reload, Deallocate, U(width), Return,
            EndCode
        ];
    }

    private static byte[] Fixture(
        bool compacted = false,
        bool laterSlotRead = false,
        bool interiorEntry = false,
        bool heapRooted = false
    )
    {
        int width = compacted ? 0 : 1;
        byte[] clear = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] spill = compacted ? [] : [Move, X(0), Y(0)];
        byte[] useSaved = laterSlotRead ? [Move, Y(0), X(0)] : [];
        byte[] skipSpill = interiorEntry ? [IsNonemptyList, F(4), X(0)] : [];
        byte[] extraLabel = interiorEntry ? [Label, U(4)] : [];
        byte root = compacted ? X(0) : Y(0);

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. clear,
            ExternalCall, U(1), U(0),
            IsList, F(3), X(0),
            .. skipSpill, .. spill, .. extraLabel,
            TestHeap, U(3), U(compacted || heapRooted ? 1 : 0),
            PutTuple, X(0), ExtendedListOperand, U(2), A(1), root,
            .. useSaved, Deallocate, U(width), Return,
            Label, U(3), TailExternalCallWithFrame, U(1), U(0), U(width),
            EndCode
        ];
    }
}
