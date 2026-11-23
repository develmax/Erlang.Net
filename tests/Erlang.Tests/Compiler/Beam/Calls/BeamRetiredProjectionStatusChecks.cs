using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetiredProjectionStatusChecks
{
    private const int SyntheticAtomCount = 2;
    internal const string TestName = "compiler/beam/reuses-the-last-projected-root-for-terminal-cleanup";

    internal static Task Run()
    {
        byte[] expected = Fixture(reused: true);
        if (Rewrite(Fixture()) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite(Fixture(sharedFailure: true)) is not null
            || Rewrite(Fixture(carrierAliasesResource: true)) is not null)
        {
            throw new InvalidOperationException("Cleanup must receive the resource and return the saved error across the trim.");
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool reused = false, bool sharedFailure = false, bool carrierAliasesResource = false)
    {
        byte[] branch = sharedFailure ? [ExactEqualTest, F(3), X(0), A(1)] : [];
        byte[] setup = reused
            ? [GetTupleElement, Y(2), U(2), X(1), GetTupleElement, X(0), U(1), Y(2), Move, Y(2), X(0)]
            : [GetTupleElement, X(0), U(1), Y(0), GetTupleElement, carrierAliasesResource ? Y(0) : Y(2), U(2), X(1),
                Move, Y(0), X(0)];
        byte[] failure = reused ? [Move, Y(2), X(0), Move, Y(0), Y(2)] : [Move, Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(3), U(1), Move, X(0), Y(2), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1), .. branch,
            ExternalCall, U(0), U(0), Move, X(0), Y(1), IsTaggedTuple, F(4), X(0), U(2), A(1),
            .. setup, Line, U(1), ExternalCall, U(2), U(1), Move, X(0), reused ? Y(0) : Y(2),
            ExactEqualTest, F(3), X(0), A(1), Move, Y(1), X(0), Deallocate, U(3), Return,
            Label, U(3), .. failure, TrimFrame, U(2), U(1), Line, U(2), ExternalCall, U(1), U(2),
            Move, Y(0), X(0), Deallocate, U(1), Return,
            Label, U(4), Deallocate, U(3), Return, EndCode
        ];
    }

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
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteRetiredProjectionForStatusCleanup",
            code,
            body
        );
    }
}
