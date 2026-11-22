using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamNestedTrySlotChecks
{
    internal const string TestName = "compiler/beam/reuses-inner-try-slots-with-an-active-outer-handler";
    private const string Module = "beam_nested_try_slot_fixture";
    private const string IncorrectCode = "Both inner handlers may share a tag slot while the outer handler and its value remain live.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        if (Rewrite(compiler, Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(compact: true))
            || Rewrite(compiler, actual) is not null || Rewrite(compiler, Fixture(wrongExit: true)) is not null
            || Rewrite(compiler, Fixture(ordinaryTagUse: true)) is not null
            || Rewrite(compiler, Fixture(thirdDepth: true)) is not null)
        {
            throw new InvalidOperationException(IncorrectCode);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool compact = false,
        bool wrongExit = false,
        bool ordinaryTagUse = false,
        bool thirdDepth = false
    )
    {
        byte width = U(compact ? 3 : 6);
        byte value = compact ? Y(0) : Y(1);
        byte outer = compact ? Y(2) : Y(5);
        byte first = compact ? Y(1) : Y(4);
        byte second = compact ? Y(1) : Y(3);
        byte[] initial = compact ? [U(3), Y(0), Y(1), Y(2)] : [U(6), Y(0), Y(1), Y(2), Y(3), Y(4), Y(5)];
        byte[] tokenRead = ordinaryTagUse ? [Move, outer, X(0)] : [];
        byte[] deeper = thirdDepth
            ? [
                TryEnter, Y(2), F(9), ExternalCall, U(1), U(0), TryEnd, Y(2), Jump, F(10),
                Label, U(9), TryCase, Y(2), Move, A(1), X(0), Label, U(10)
            ]
            : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, width, U(1), InitYRegisters, ExtendedListOperand, .. initial,
            Move, X(0), value, .. tokenRead, TryEnter, outer, F(8),
            TryEnter, first, F(3), .. deeper, ExternalCall, U(1), U(0),
            TryEnd, wrongExit ? outer : first, Jump, F(4),
            Label, U(3), TryCase, first, Move, value, X(0), ExternalCall, U(1), U(0),
            Label, U(4), Move, value, X(0),
            TryEnter, second, F(5), ExternalCall, U(1), U(0), TryEnd, second, Jump, F(6),
            Label, U(5), TryCase, second, Move, A(1), X(0),
            Label, U(6), TryEnd, outer, Deallocate, width, Return,
            Label, U(8), TryCase, outer, Move, value, X(0), Deallocate, width, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "CompactDisjointTrySlotLifetimes",
            code,
            11
        );
}
