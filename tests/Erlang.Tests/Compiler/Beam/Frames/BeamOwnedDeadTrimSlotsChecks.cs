using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOwnedDeadTrimSlotsChecks
{
    internal const string TestName = "compiler/beam/compacts-dead-slots-across-an-owned-trim";
    private const string Pass = "CompactDeadSlotsThroughOwnedTrim";
    private const string InvalidLayout = "Both stack epochs must retain the same live values after compaction.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_owned_dead_trim_slots");
        if (Rewrite(compiler, Fixture()) is not { } output
            || !output.SequenceEqual(Fixture(compact: true))
            || Rewrite(compiler, output) is not null
            || Rewrite(compiler, Fixture(conflictingEdge: true)) is not null
            || Rewrite(compiler, MultipleEpochFixture()) is not { } multiple
            || !multiple.SequenceEqual(MultipleEpochFixture(compact: true))
            || Rewrite(compiler, multiple) is not null)
        {
            throw new InvalidOperationException(InvalidLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, byte[] bytes) =>
        (byte[]?)Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(bytes),
            5
        );

    private static byte[] MultipleEpochFixture(bool compact = false)
    {
        byte width = U(compact ? 2 : 6);
        byte finalSlot = Y(compact ? 1 : 5);
        byte endSlot = Y(compact ? 0 : 1);
        byte[] initial = compact ? [U(1), Y(1)] : [U(4), Y(2), Y(3), Y(4), Y(5)];
        byte[] discarded = compact ? [] : [Move, X(1), Y(1)];
        byte[] firstTrim = compact ? [TrimFrame, U(1), U(1)] : [TrimFrame, U(2), U(4)];
        byte[] secondTrim = compact ? [] : [TrimFrame, U(2), U(2)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ), Allocate, width, U(2), InitYRegisters, ExtendedListOperand, .. initial,
            Move, X(0), Y(0), .. discarded, Move, X(1), finalSlot, Move, Y(0), X(0),
            .. firstTrim, Move, Y(compact ? 0 : 3), X(0), .. secondTrim,
            Move, endSlot, X(0), Deallocate, U(compact ? 1 : 2), Return, EndCode
        ];
    }

    private static byte[] Fixture(bool compact = false, bool conflictingEdge = false)
    {
        byte width = U(compact ? 2 : 4);
        byte dropped = U(compact ? 1 : 2);
        byte kept = U(compact ? 1 : 2);
        byte stored = Y(compact ? 1 : 2);
        byte[] initial = compact ? [] : [InitYRegisters, ExtendedListOperand, U(2), Y(1), Y(3)];
        byte[] jump = conflictingEdge ? [IsAtom, F(3), X(0)] : [];
        byte[] join = conflictingEdge ? [Label, U(3)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ), Allocate, width, U(2), .. initial,
            Move, X(0), Y(0), Move, X(1), stored, ExternalCall, U(2), U(0),
            Move, Y(0), X(0), .. jump, TrimFrame, dropped, kept, .. join,
            Move, Y(0), X(0), Deallocate, kept, Return, EndCode
        ];
    }
}
