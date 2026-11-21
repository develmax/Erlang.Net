using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTupleTagScratchChecks
{
    internal const string TestName = "compiler/beam/keeps-a-tuple-in-its-entry-register-during-a-tag-test";
    private const string UnexpectedRewrite = "Tuple tag scratch replacement changed a live branch value.";
    private const int Entry = 2;
    private const int Fallback = 3;
    private const int Labels = 4;

    internal static Task Run()
    {
        if (Rewrite(Body()) is not { } rewritten || !rewritten.SequenceEqual(Body(rewritten: true))
            || Rewrite(Body(liveTagOnFailure: true)) is not null
            || Rewrite(Body(liveTupleOnSuccess: true)) is not null
            || Rewrite(Projection()) is not { } direct || !direct.SequenceEqual(Projection(rewritten: true))
            || Rewrite(Projection(liveAlias: true)) is not null)
        {
            throw new InvalidOperationException(UnexpectedRewrite);
        }

        return Task.CompletedTask;
    }

    private static byte[] Body(
        bool rewritten = false,
        bool liveTagOnFailure = false,
        bool liveTupleOnSuccess = false
    )
    {
        byte[] projection = rewritten
            ? [
                GetTupleElement, X(0), U(0), X(1),
                ExactEqualTest, F(Fallback), X(1), A(1),
                GetTupleElement, X(0), U(1), X(0)
            ] : [
                Move, X(0), X(1),
                GetTupleElement, X(1), U(0), X(0),
                ExactEqualTest, F(Fallback), X(0), A(1),
                GetTupleElement, X(1), U(1), X(0)
            ];
        byte[] success = liveTupleOnSuccess ? [Move, X(1), X(0)] : [];
        byte[] rejected = liveTagOnFailure ? [] : [Move, A(2), X(0)];

        return
        [
            .. Start(
                1,
                Entry,
                2,
                1
            ),
            IsTuple, F(Fallback), X(0), TestArity, F(Fallback), X(0), U(2),
            .. projection, .. success, Return,
            Label, U(Fallback), .. rejected, Return, EndCode
        ];
    }

    private static byte[] Projection(bool rewritten = false, bool liveAlias = false)
    {
        byte[] read = rewritten ? [GetTupleElement, X(0), U(1), X(0)]
            : [Move, X(0), X(1), GetTupleElement, X(1), U(1), X(0)];
        byte[] alias = liveAlias ? [Move, X(1), X(0)] : [];

        return
        [
            .. Start(
                1,
                Entry,
                2,
                1
            ),
            IsTuple, F(Fallback), X(0), TestArity, F(Fallback), X(0), U(2),
            .. read, .. alias, Return,
            Label, U(Fallback), Move, A(2), X(0), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            Labels,
            1,
            code
        );
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteTupleTagScratchSlots",
            code,
            instructions
        );
    }
}
