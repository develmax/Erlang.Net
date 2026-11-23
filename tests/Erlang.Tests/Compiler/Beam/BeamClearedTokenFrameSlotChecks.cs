using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClearedTokenFrameSlotChecks
{
    internal const string TestName = "compiler/beam/reuses-a-cleared-frame-slot-for-a-checked-token";
    private const int SyntheticAtomCount = 3;
    private const string RewriteMethod = "RewriteClearedTokenFrameSlot";
    private const string ChangedCapture = "Checked token extraction chose an incorrect frame slot.";

    internal static void Run()
    {
        byte[] before = Body(1, 3, 4);
        byte[] expected = Body(3, 3, 4);
        byte[] retainedHigher = Body(1, 3, 3);
        byte[] differentFailure = Body(1, 4, 4);

        if (Rewrite(before) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(retainedHigher) is not null
            || Rewrite(differentFailure) is not null)
        {
            throw new InvalidOperationException(ChangedCapture);
        }
    }

    private static byte[] Body(int tokenSlot, int reject, int forwardedSlot) =>
    [
        .. Start(
            1,
            2,
            1,
            3
        ),
        Allocate, U(5), U(3),
        InitYRegisters, ExtendedListOperand, U(2), Y(1), Y(3),
        ExternalCall, U(1), U(0),
        IsTaggedTuple, F(3), X(0), U(2), A(2),
        GetTupleElement, X(0), U(1), X(0),
        IsNonemptyList, F(3), X(0),
        GetList, X(0), X(1), X(0),
        IsNonemptyList, F(3), X(0),
        GetList, X(0), Y(tokenSlot), X(0),
        IsNil, F(reject), X(0),
        Move, X(1), X(0),
        ExternalCall, U(1), U(1),
        TestHeap, U(3), U(1),
        PutTuple, X(2), ExtendedListOperand, U(2), X(0), Y(tokenSlot),
        Jump, F(4),
        Label, U(3),
        Move, ExtendedLiteralOperand, U(0), X(2),
        Label, U(4),
        Move, Y(forwardedSlot), X(1),
        Move, Y(2), X(0),
        TailLocalCallWithFrame, U(3), F(2), U(5),
        EndCode
    ];

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            5,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            SyntheticAtomCount,
            1,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            RewriteMethod,
            code,
            body
        );
    }
}
