using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLiteralGuardedTryFrameChecks
{
    internal const string TestName = "compiler/beam/places-independent-protected-frames-after-literal-entry-guard";
    private const string Module = "beam_literal_guarded_try_frame_fixture";
    private const string WrongLayout = "Each protected arm must allocate and release its own live frame after the entry guard.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        if (Rewrite(compiler, Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(split: true))
            || Rewrite(compiler, actual) is not null
            || Rewrite(compiler, Fixture(hotUsesOriginal: true)) is not null
            || Rewrite(compiler, Fixture(coldUsesOriginalAgain: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool split = false,
        bool hotUsesOriginal = false,
        bool coldUsesOriginalAgain = false
    )
    {
        const int HotHandler = 3;
        const int Other = 4;
        const int ColdHandler = 5;
        byte hotWidth = U(split ? 2 : 4);
        byte coldWidth = U(split ? 1 : 4);
        byte hotSaved = split ? Y(0) : Y(2);
        byte coldToken = split ? Y(0) : Y(1);
        byte[] entry = split
            ? [ExactEqualTest, F(Other), X(0), A(1), Allocate, hotWidth, U(0), InitYRegisters, ExtendedListOperand, U(1), Y(0)]
            : [
                Allocate, hotWidth, U(1), InitYRegisters, ExtendedListOperand, U(3), Y(1), Y(2), Y(3),
                Move, X(0), Y(0), ExactEqualTest, F(Other), Y(0), A(1)
            ];
        byte[] coldAllocate = split ? [Allocate, coldWidth, U(1)] : [];
        byte[] coldReload = split ? [] : [Move, Y(0), X(0)];
        byte[] hotUse = hotUsesOriginal ? [Move, Y(0), X(1)] : [];
        byte[] coldUse = coldUsesOriginalAgain ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            .. entry, TryEnter, Y(1), F(HotHandler), Move, A(1), X(0), .. hotUse,
            ExternalCall, U(hotUsesOriginal ? 2 : 1), U(0), Move, X(0), hotSaved,
            ExternalCall, U(0), U(1), Move, hotSaved, X(0), TryEnd, Y(1), Deallocate, hotWidth, Return,
            Label, U(HotHandler), TryCase, Y(1), Move, A(1), X(0), Deallocate, hotWidth, Return,
            Label, U(Other), .. coldAllocate, TryEnter, coldToken, F(ColdHandler), .. coldReload,
            Line, U(1), ExternalCall, U(1), U(0), .. coldUse, TryEnd, coldToken, Deallocate, coldWidth, Return,
            Label, U(ColdHandler), TryCase, coldToken, Move, A(1), X(0), Deallocate, coldWidth, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "SplitLiteralGuardedTryFrames",
            code,
            6
        );
}
