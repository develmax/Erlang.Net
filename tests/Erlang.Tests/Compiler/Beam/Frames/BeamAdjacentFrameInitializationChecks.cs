using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamAdjacentFrameInitializationChecks
{
    internal const string TestName = "compiler/beam/merges-adjacent-initializations-of-one-frame";
    private const string Failure = "Adjacent frame clears must keep all slots in ascending order.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_adjacent_frame_initializations");
        byte[] header = [.. Start(
            1,
            2,
            1,
            1
        ), Allocate, U(4), U(1)];
        byte[] tail = [Move, Y(0), X(0), Deallocate, U(4), Return, EndCode];
        byte[] before =
        [
            .. header,
            InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(2),
            InitYRegisters, ExtendedListOperand, U(2), Y(1), Y(2),
            InitYRegisters, ExtendedListOperand, U(1), Y(3),
            .. tail
        ];
        byte[] expected =
        [
            .. header,
            InitYRegisters, ExtendedListOperand, U(4), Y(0), Y(1), Y(2), Y(3),
            .. tail
        ];
        byte[] split =
        [
            .. header,
            InitYRegisters, ExtendedListOperand, U(4), Y(0), Y(1), Y(2), Y(3),
            Move, X(0), Y(1),
            InitYRegisters, ExtendedListOperand, U(1), Y(1),
            .. tail
        ];
        if (Rewrite(before) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(split) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "MergeAdjacentFrameInitializations",
            code,
            3
        );
    }
}
