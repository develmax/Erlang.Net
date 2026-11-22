using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamGuardProvenLiteralMoveChecks
{
    internal const string TestName = "compiler/beam/removes-literal-moves-proven-by-a-guard";
    private const string Failure = "Remove only exact fallthrough copies and preserve locations and unreached alternatives.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_guard_proven_literal_move_probe");
        byte[] before =
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsNil, F(3), X(0), Line, U(1), Move, A(0), X(0), Return,
            Label, U(3), ExactEqualTest, F(4), I(7), X(1), Move, I(7), X(1), Move, X(1), X(0), Return,
            Label, U(4), Move, I(0), X(0), Return, EndCode
        ];
        byte[] expected =
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsNil, F(3), X(0), Line, U(1), Return,
            Label, U(3), ExactEqualTest, F(4), I(7), X(1), Move, X(1), X(0), Return,
            Label, U(4), Move, I(0), X(0), Return, EndCode
        ];
        byte[] conflated =
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsNil, F(3), X(0), Move, A(0), X(1), Return,
            Label, U(3), NumericEqualTest, F(4), X(0), I(7), Move, I(7), X(0), Return,
            Label, U(4), ExactEqualTest, F(5), X(0), I(7),
            Label, U(5), Move, I(7), X(0), Return, EndCode
        ];
        if (Rewrite(before, 5) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected, 5) is not null || Rewrite(conflated, 6) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code, int labels) => (byte[]?)Call(
            compiler,
            "RemoveGuardProvenLiteralMoves",
            code,
            labels
        );
    }
}
