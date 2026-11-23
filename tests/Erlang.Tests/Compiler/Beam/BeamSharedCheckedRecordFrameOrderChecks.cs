using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedCheckedRecordFrameOrderChecks
{
    internal const string TestName = "compiler/beam/orders-shared-record-roots-by-ssa-field-identity";
    private const string Failure = "The record save order changed a live source or a retained slot.";

    internal static Task Run()
    {
        const int badShape = 1;
        const int entry = 2;
        const int retry = 3;
        byte[] prefix =
        [
            .. Start(
                badShape,
                entry,
                2,
                2
            ),
            IsTaggedTuple, F(badShape), X(0), U(4), A(3),
            GetTupleElement, X(0), U(1), X(2),
            GetTupleElement, X(0), U(2), X(3),
            GetTupleElement, X(0), U(3), X(0),
            ExactEqualTest, F(retry), X(2), A(4),
            AllocateHeap, U(3), U(4), U(4)
        ];
        byte[] oldSaves = [Move, X(1), Y(0), Move, X(3), Y(1), Move, X(0), Y(2)];
        byte[] newSaves = [Move, X(3), Y(0), Move, X(0), Y(1), Move, X(1), Y(2)];
        byte[] afterTrim =
        [
            TrimFrame, U(2), U(1), ExternalCall, U(2), U(1), Move, Y(0), X(0), Deallocate, U(1), Return,
            Label, U(retry), Return, EndCode
        ];
        byte[] preparation =
        [
            PutList, Y(1), A(0), X(0), PutList, Y(2), X(0), X(2), ExternalCall, U(1), U(0),
            Move, Y(2), X(1), Move, Y(1), X(0)
        ];
        byte[] original = [.. prefix, .. oldSaves, .. preparation, Move, Y(0), Y(2), .. afterTrim];
        byte[] expected =
        [
            .. prefix, .. newSaves, PutList, X(3), A(0), X(0), PutList, Y(1), X(0), X(2),
            ExternalCall, U(1), U(0), Move, Y(1), X(1), Move, Y(0), X(0), .. afterTrim
        ];
        if (Rewrite(original) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite([.. prefix, .. oldSaves, .. preparation, Move, Y(1), Y(2), .. afterTrim]) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] instructions)
    {
        const int labels = 4;
        ReadOnlyMemory<byte> code = instructions;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            labels,
            1,
            code
        );
        var operands = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> capture = (start, end, slot) => operands.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            null,
            capture
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "OrderSharedCheckedRecordCallFrames",
            code,
            body,
            operands
        );
    }
}
