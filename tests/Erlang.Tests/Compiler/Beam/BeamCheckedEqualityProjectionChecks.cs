using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedEqualityProjectionChecks
{
    internal const string TestName = "compiler/beam/reuses-the-checked-payload-across-an-equality-test";
    private const string WrongLayout = "The reused record projection changed an observed carrier, scratch, or branch owner.";

    internal static Task Run()
    {
        if (Rewrite(Image(shortened: false)) is not { } result || !result.SequenceEqual(Image(shortened: true))
            || Rewrite(Image(shortened: false, successReadsCarrier: true)) is not null
            || Rewrite(Image(shortened: false, failureReadsScratch: true)) is not null
            || Rewrite(Image(shortened: false, sharedFailure: true)) is not null
            || Rewrite(result) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Image(
        bool shortened,
        bool successReadsCarrier = false,
        bool failureReadsScratch = false,
        bool sharedFailure = false
    )
    {
        byte[] otherEdge = sharedFailure ? [IsTuple, F(3), X(0)] : [];
        byte[] accepted = successReadsCarrier ? [Move, X(0), X(2)] : [];
        byte[] repeated = shortened ? [] : [GetTupleElement, X(0), U(1), X(0)];
        byte[] observe = failureReadsScratch ? [Move, X(1), X(0)] : [Move, A(4), X(1)];
        byte result = shortened ? X(0) : X(1);

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            .. otherEdge, IsTaggedTuple, F(4), X(0), U(2), A(2),
            GetTupleElement, X(0), U(1), result, ExactEqualTest, F(3), result, A(3),
            .. accepted, Move, A(4), X(0), Return,
            Label, U(3), .. repeated, .. observe, Return,
            Label, U(4), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            5,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteCheckedEqualityProjection",
            image,
            body
        );
    }
}
