using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTransientOutgoingSlotChecks
{
    internal const string TestName = "compiler/beam/compacts-fully-transient-outgoing-slots";
    private const string Pass = "CompactTransientOutgoingSlots";
    private const string InvalidLayout = "Compaction must preserve outgoing values and shrink the entire frame owner.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_transient_outgoing_slot");
        if (Rewrite(compiler, Fixture()) is not { } output
            || !output.SequenceEqual(Fixture(compact: true))
            || Rewrite(compiler, output) is not null
            || Rewrite(compiler, Fixture(intervening: [Move, X(1), X(2)])) is not null
            || Rewrite(compiler, Fixture(intervening: [TestHeap, U(2), U(1)])) is not null
            || Rewrite(compiler, Fixture(intervening: [Label, U(4)])) is not null
            || Rewrite(compiler, Fixture(fallbackUsesSlot: true)) is not null
            || Rewrite(compiler, Fixture(trim: true)) is not null)
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

    private static byte[] Fixture(
        bool compact = false,
        byte[]? intervening = null,
        bool fallbackUsesSlot = false,
        bool trim = false
    )
    {
        byte width = U(compact ? 1 : 2);
        byte target = compact ? X(1) : Y(1);
        byte[] initial = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] reload = compact ? [] : [Move, Y(1), X(1)];
        byte[] fallback = fallbackUsesSlot ? [Move, Y(1), X(0)] : [];
        byte[] deallocate = trim ? [TrimFrame, U(1), U(1), Deallocate, U(1)] : [Deallocate, width];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ), Allocate, width, U(2),
            Move, X(1), Y(0), .. initial,
            Move, X(0), target, .. (intervening ?? []), Move, Y(0), X(0), .. reload,
            DynamicApply, U(0),
            IsTuple, F(3), X(0),
            TestArity, F(3), X(0), U(1),
            GetTupleElement, X(0), U(0), target, Move, Y(0), X(0), .. reload,
            ExternalCall, U(2), U(0), .. deallocate, Return,
            Label, U(3), .. fallback, Deallocate, width, Return, EndCode
        ];
    }
}
