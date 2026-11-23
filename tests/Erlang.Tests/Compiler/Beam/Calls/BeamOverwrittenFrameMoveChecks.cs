using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOverwrittenFrameMoveChecks
{
    internal const string TestName = "compiler/beam/removes-only-frame-writes-overwritten-before-a-root-scan";
    private const string RewriteMethod = "RewriteOverwrittenFrameMoves";
    private const string InvalidClear = "The frame write's consumer or collecting boundary was lost.";

    internal static void Run()
    {
        byte[] original = Sample(keepFirst: true, source: X(0), collect: []);
        byte[] expected = Sample(keepFirst: false, source: X(0), collect: []);
        byte[] readsOld = Sample(keepFirst: true, source: Y(0), collect: []);
        byte[] scans = Sample(keepFirst: true, source: X(0), collect: [TestHeap, U(3), U(1)]);
        if (Rewrite(original) is not { } value || !value.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(readsOld) is not null || Rewrite(scans) is not null)
        {
            throw new InvalidOperationException(InvalidClear);
        }
    }

    private static byte[] Sample(bool keepFirst, byte source, byte[] collect)
    {
        byte[] saved = keepFirst ? [Move, X(0), Y(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            AllocateHeap, U(1), U(3), U(1),
            .. saved, .. collect,
            PutTuple, Y(0), ExtendedListOperand, U(2), source, I(1),
            Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            code
        );
        object body = Call(
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
            RewriteMethod,
            code,
            body
        );
    }
}
