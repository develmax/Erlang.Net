using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamUnchangedCallSpillChecks
{
    internal const string TestName = "compiler/beam/removes-unchanged-register-spills-before-calls";
    private const string Failure = "Preserve outgoing arguments and remove only a provably unchanged stack spill.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_unchanged_call_spill_probe");
        byte[] before =
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(1), U(3), Move, X(0), Y(0), Move, X(2), X(1), Move, Y(0), X(0), TrimFrame, U(1), U(0),
            Line, U(1), ExternalCall, U(2), U(0), Deallocate, U(0), Return, EndCode
        ];
        byte[] expected =
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Move, X(2), X(1), Allocate, U(0), U(2),
            Line, U(1), ExternalCall, U(2), U(0), Deallocate, U(0), Return, EndCode
        ];
        byte[] clobbered =
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(1), U(3), Move, X(0), Y(0), Move, X(2), X(0), Move, Y(0), X(0), TrimFrame, U(1), U(0),
            Line, U(1), ExternalCall, U(2), U(0), Deallocate, U(0), Return, EndCode
        ];
        byte[] collecting =
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(1), U(3), Move, X(0), Y(0), Move, X(2), X(1), TestHeap, U(2), U(3),
            Move, Y(0), X(0), TrimFrame, U(1), U(0),
            Line, U(1), ExternalCall, U(2), U(0), Deallocate, U(0), Return, EndCode
        ];
        if (Rewrite(before) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(clobbered) is not null || Rewrite(collecting) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "RemoveUnchangedCallSpills",
            code,
            3
        );
    }
}
