using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTrimBeforeCompleteReleaseChecks
{
    internal const string TestName = "compiler/beam/relocates-live-roots-before-a-complete-frame-release";
    private const string Failure = "A preceding trim must remap retained roots and reduce the final complete release.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_trim_before_complete_release");
        if (Rewrite(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(trimmed: true))
            || Rewrite(Fixture(partial: true)) is not null
            || Rewrite(Fixture(usesClearedSlot: true)) is not null
            || Rewrite(Fixture(closure: false)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        if (Schedule(TwoResults()) is not { } split || !split.SequenceEqual(TwoResults(trimmed: true))
            || Schedule(TwoResults(intervening: true)) is not null || Schedule(TwoResults(partial: true)) is not null
            || Schedule(TwoResults(usesClearedSlot: true)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        byte[]? Schedule(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ScheduleTwoResultFrameTrims",
            code,
            4
        );

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "TrimStackBetweenCalls",
            code,
            4
        );
    }

    private static byte[] TwoResults(
        bool trimmed = false,
        bool intervening = false,
        bool partial = false,
        bool usesClearedSlot = false
    )
    {
        byte[] release = trimmed ? [TrimFrame, U(1), U(1)] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] extra = intervening ? [ExternalCall, U(0), U(0)] : [];
        byte[] later = usesClearedSlot ? [Move, Y(1), X(2)] : [];
        int finalRemoved = trimmed || partial ? 1 : 2;
        int finalWidth = partial ? 1 : 0;

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Allocate, U(2), U(0), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            Line, U(1), LocalCall, U(0), F(2), Move, X(0), Y(trimmed ? 1 : 0),
            Line, U(2), LocalCall, U(0), F(2), Move, X(0), Y(trimmed ? 0 : 1),
            Line, U(3), LocalCall, U(0), F(2), Move, X(0), X(1), Move, Y(trimmed ? 0 : 1), X(0),
            .. release, Line, U(4), ExternalCall, U(2), U(0), .. extra, .. later,
            Move, X(0), X(1), Move, Y(0), X(0), TrimFrame, U(finalRemoved), U(finalWidth),
            ExternalCall, U(2), U(0), Deallocate, U(finalWidth), Return, EndCode
        ];
    }

    private static byte[] Fixture(
        bool trimmed = false,
        bool partial = false,
        bool usesClearedSlot = false,
        bool closure = true
    )
    {
        byte[] atCall = trimmed
            ? [Move, Y(0), Y(1), TrimFrame, U(1), U(2)]
            : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] furtherRead = usesClearedSlot ? [Move, Y(1), X(2)] : [];
        byte[] closureSave = closure
            ? [
                TestHeap, (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
                U(AllocationWords), U(0), U(AllocationFloats), U(0), U(AllocationFuns), U(1), U(2),
                MakeFun, U(0), Y(1), ExtendedListOperand, U(0)
            ]
            : [Move, X(1), Y(1)];
        int removed = partial ? 1 : trimmed ? 2 : 3;
        int remaining = partial ? 2 : 0;

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(3), U(2),
            Move, X(0), Y(0), Move, X(0), Y(2), .. closureSave,
            LocalCall, U(2), F(2),
            Move, X(0), X(1), Move, Y(1), X(0),
            .. atCall, Line, U(1),
            LocalCall, U(2), F(2),
            .. furtherRead,
            Move, Y(0), X(0), Move, Y(trimmed ? 1 : 2), X(1),
            TrimFrame, U(removed), U(remaining),
            TailLocalCallWithFrame, U(2), F(2), U(remaining),
            EndCode
        ];
    }
}
