using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamZeroFramePathChecks
{
    internal const string TestName = "compiler/beam/places-zero-slot-frames-only-on-calling-paths";
    private const string Failure = "Only the ordinary call branch may retain the zero-slot frame.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_zero_frame_path_probe");
        byte[] before =
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(0), U(1), IsAtom, F(3), X(0), Deallocate, U(0), Return,
            Label, U(3), Line, U(1), ExternalCall, U(1), U(0), Deallocate, U(0), Return, EndCode
        ];
        byte[] expected =
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsAtom, F(3), X(0), Return,
            Label, U(3), Allocate, U(0), U(1), Line, U(1), ExternalCall, U(1), U(0), Deallocate, U(0), Return, EndCode
        ];
        byte[] incompatibleJoin =
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(0), U(1), IsAtom, F(3), X(0), Jump, F(4),
            Label, U(3), Line, U(1), ExternalCall, U(1), U(0),
            Label, U(4), Deallocate, U(0), Return, EndCode
        ];
        byte[] backEdge =
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(0), U(1), IsAtom, F(3), X(0), Jump, F(2),
            Label, U(3), ExternalCall, U(1), U(0), Deallocate, U(0), Return, EndCode
        ];
        if (Rewrite(before, 4) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected, 4) is not null || Rewrite(incompatibleJoin, 5) is not null || Rewrite(backEdge, 4) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code, int labels) => (byte[]?)Call(
            compiler,
            "PlaceZeroFramesOnCallingPaths",
            code,
            labels
        );
    }
}
