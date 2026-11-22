using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCoalescedTaggedGuardChecks
{
    internal const string TestName = "compiler/beam/coalesces-tuple-shape-and-dead-tag-checks";
    private const string WrongBranch = "Coalescing a tag check must preserve each failure edge and any live projected field.";

    internal static Task Run()
    {
        if (Coalesce(Fixture(fused: false)) is not { } result || !result.SequenceEqual(Fixture(fused: true))
            || Coalesce(Fixture(fused: false, readsTag: true)) is not null
            || Coalesce(Fixture(fused: false, otherFailure: true)) is not null
            || Coalesce(result) is not null)
        {
            throw new InvalidOperationException(WrongBranch);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool fused, bool readsTag = false, bool otherFailure = false)
    {
        byte[] matcher = fused
            ? [IsTaggedTuple, F(3), X(0), U(2), A(1)]
            : [
                IsTuple, F(3), X(0), TestArity, otherFailure ? F(1) : F(3), X(0), U(2),
                GetTupleElement, X(0), U(0), X(1), ExactEqualTest, F(3), X(1), A(1)
            ];
        byte[] failure = readsTag ? [Move, X(1), X(0)] : [Move, A(2), X(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            .. matcher, Move, A(1), X(0), Return,
            Label, U(3), .. failure, Return, EndCode
        ];
    }

    private static byte[]? Coalesce(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            4,
            1,
            image
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
            "RewriteTaggedTupleGuards",
            image,
            body
        );
    }
}
