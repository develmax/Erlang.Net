using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedCallProjectionFrameChecks
{
    internal const string TestName = "compiler/beam/keeps-checked-payload-carrier-rooted-at-call-entry";
    private const string Failure = "The checked carrier and independent saved argument must keep their original values.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_checked_call_projection_frame");
        if (Rewrite(Fixture()) is not { } rewritten || !rewritten.SequenceEqual(Fixture(placed: true))
            || Rewrite(rewritten) is not null || Rewrite(Fixture(savesProjectedValue: true)) is not null
            || Rewrite(Fixture(differentRetry: true)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        CheckLiteralReturn(compiler);

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "PlaceCheckedCallProjectionAfterFrame",
            code,
            5
        );
    }

    private static void CheckLiteralReturn(BeamModuleCompiler compiler)
    {
        int error = (int)Call(
            compiler,
            "Import",
            "erlang",
            "error",
            1
        );
        int ordinary = (int)Call(
            compiler,
            "Import",
            "other_module",
            "error",
            1
        );
        foreach (bool throws in new[] { false, true })
        {
            if (Rewrite(Shape(throws)) is not { } result
                || !result.SequenceEqual(Shape(throws, moved: true))
                || Rewrite(result) is not null
                || Rewrite(Shape(throws, sharedFailure: true)) is not null)
            {
                throw new InvalidOperationException(Failure);
            }
        }

        if (Rewrite(Shape(throws: true, remote: true)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "LocalizeCheckedLiteralReturnFrame",
            code,
            5
        );

        byte[] Shape(
            bool throws,
            bool moved = false,
            bool sharedFailure = false,
            bool remote = false
        )
        {
            byte[] setup = [Move, X(0), Y(0)];
            byte[] frame = moved ? [] : [Allocate, U(1), U(1), .. setup];
            byte[] recovered = moved ? [Allocate, U(1), U(2), .. setup, Move, X(1), X(0)] : [];
            byte[] exit = throws
                ? (moved
                    ? [Line, U(1), TailExternalCall, U(1), U(error)]
                    : [Line, U(1), ExternalCall, U(1), U(remote ? ordinary : error), Jump, F(4)])
                : (moved ? [Return] : [Deallocate, U(1), Return]);
            byte[] tail = throws ? [Label, U(4), Deallocate, U(1), Return] : [Deallocate, U(1), Return];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    1
                ),
                IsTaggedTuple, F(sharedFailure ? 3 : 1), X(0), U(2), A(1), .. frame,
                GetTupleElement, X(0), U(1), moved ? X(1) : X(0),
                ExactEqualTest, F(3), moved ? X(1) : X(0), A(1),
                Move, A(1), X(0), .. exit,
                Label, U(3), .. recovered, Move, X(0), X(1), Move, Y(0), X(0), .. tail, EndCode
            ];
        }
    }

    private static byte[] Fixture(
        bool placed = false,
        bool savesProjectedValue = false,
        bool differentRetry = false
    )
    {
        byte[] frame = [Allocate, U(1), U(2), Move, X(savesProjectedValue ? 0 : 1), Y(0)];
        byte[] projection = [GetTupleElement, X(0), U(3), X(0), Line, U(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(3), X(0), U(4), A(1),
            GetTupleElement, X(0), U(2), X(2),
            ExactEqualTest, F(differentRetry ? 4 : 3), X(2), A(1),
            .. (placed ? frame : projection),
            .. (placed ? projection : frame),
            ExternalCall, U(1), U(0),
            Move, Y(0), X(1),
            TailExternalCallWithFrame, U(2), U(0), U(1),
            Label, U(3), Move, X(1), X(0), Return,
            Label, U(4), Return,
            EndCode
        ];
    }
}
