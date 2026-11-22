using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSerialTrySlotChecks
{
    internal const string TestName = "compiler/beam/reuses-disjoint-try-tag-stack-slots";
    private const string Module = "beam_serial_try_slot_fixture";
    private const string IncorrectCode = "The compiler changed a live exception tag or a value held across a protected call.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        if (Rewrite(compiler, Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(compact: true))
            || Rewrite(compiler, actual) is not null
            || Rewrite(compiler, Fixture(nested: true)) is not null
            || Rewrite(compiler, Fixture(ordinaryTagUse: true)) is not null)
        {
            throw new InvalidOperationException(IncorrectCode);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool compact = false, bool nested = false, bool ordinaryTagUse = false)
    {
        byte width = U(compact ? 2 : 4);
        byte value = compact ? Y(0) : Y(1);
        byte first = compact ? Y(1) : Y(3);
        byte second = compact ? Y(1) : Y(2);
        byte[] initial = compact ? [U(2), Y(0), Y(1)] : [U(4), Y(0), Y(1), Y(2), Y(3)];
        byte[] exit = nested ? [] : [TryEnd, first];
        byte[] tokenRead = ordinaryTagUse ? [Move, first, X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, width, U(1), InitYRegisters, ExtendedListOperand, .. initial,
            Move, X(0), value, .. tokenRead,
            TryEnter, first, F(3), ExternalCall, U(1), U(0), .. exit, Jump, F(4),
            Label, U(3), TryCase, first, Move, A(1), X(0),
            Label, U(4), Move, value, X(0),
            TryEnter, second, F(5), ExternalCall, U(1), U(0), TryEnd, second, Jump, F(6),
            Label, U(5), TryCase, second, Move, A(1), X(0),
            Label, U(6), Deallocate, width, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "CompactDisjointTrySlotLifetimes",
            code,
            7
        );
}
