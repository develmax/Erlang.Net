using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSavedEqualityRetryProjectionChecks
{
    internal const string TestName = "compiler/beam/shares-a-saved-field-on-an-exclusive-equality-retry";

    internal static Task Run()
    {
        if (Rewrite(Fixture()) is not { } result || !result.AsSpan().SequenceEqual(Fixture(shared: true))
            || Rewrite(Fixture(shared: true)) is not null
            || Rewrite(Fixture(otherEdge: true)) is not null
            || Rewrite(Fixture(wrongField: true)) is not null
            || Rewrite(Fixture(insufficientRoots: true)) is not null)
        {
            throw new InvalidOperationException("Only the same rooted field can be shared with the exclusive retry.");
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool shared = false,
        bool otherEdge = false,
        bool wrongField = false,
        bool insufficientRoots = false
    )
    {
        byte[] extra = otherEdge ? [IsTuple, F(3), X(0)] : [];
        byte[] heap = insufficientRoots ? [TestHeap, U(0), U(1)] : [];
        byte[] repeated = shared ? [] : [GetTupleElement, Y(0), U(wrongField ? 1 : 0), X(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(1), U(2), Move, X(1), Y(0),
            .. extra,
            GetTupleElement, Y(0), U(0), X(1), ExactEqualTest, F(3), X(0), X(1),
            Deallocate, U(1), Return,
            Label, U(3), .. heap, .. repeated, Move, X(1), X(0), Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            4,
            1,
            code
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
            "RewriteFailureProjectionJoinsCore",
            code,
            body,
            Array.Empty<(int, int, int)>(),
            false,
            false,
            true
        );
    }
}
