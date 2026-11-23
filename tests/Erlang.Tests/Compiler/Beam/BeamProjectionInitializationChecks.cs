using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectionInitializationChecks
{
    internal const string TestName = "compiler/beam/skips-a-projection-slot-initialized-before-the-first-root-scan";
    private const string CandidateRewrite = "RewriteProjectionInitializations";
    private const string InvalidInitialization = "Unexpected change to a stack clear before a projection or a frame scan.";

    internal static void Run()
    {
        byte[] projection = [GetTupleElement, X(0), U(0), Y(1)];
        byte[] clearTwo = [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)];
        byte[] clearOne = [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] start = [.. Start(
            1,
            2,
            1,
            1
        ), Allocate, U(2), U(1)];
        byte[] exit = [Deallocate, U(2), Return, EndCode];
        byte[] positive = [.. start, .. clearTwo, .. projection, .. exit];
        byte[] expected = [.. start, .. clearOne, .. projection, .. exit];
        byte[] scanFirst = [.. start, .. clearTwo, TestHeap, U(3), U(1), .. projection, .. exit];
        byte[] readFirst = [.. start, .. clearTwo, Move, Y(1), X(1), .. projection, .. exit];
        byte[] readOnProjection = [.. start, .. clearTwo, GetTupleElement, Y(1), U(0), Y(1), .. exit];

        if (Rewrite(positive) is not { } rewritten || !rewritten.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(scanFirst) is not null
            || Rewrite(readFirst) is not null || Rewrite(readOnProjection) is not null)
        {
            throw new InvalidOperationException(InvalidInitialization);
        }
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
            CandidateRewrite,
            code,
            body
        );
    }
}
