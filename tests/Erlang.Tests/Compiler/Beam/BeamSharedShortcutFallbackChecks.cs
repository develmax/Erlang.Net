using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedShortcutFallbackChecks
{
    internal const string TestName = "compiler/beam/shares-a-literal-fallback-across-a-shortcut-case";
    private const string WrongLayout = "The two closed fallbacks must share one stack result before the continuation.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_shared_shortcut_fallback");
        _ = Call(compiler, "Intern", "opt");
        _ = Call(compiler, "Intern", "base");
        if (Apply(Fixture()) is not ValueTuple<ReadOnlyMemory<byte>, int> shared
            || shared.Item2 != 5 || !shared.Item1.Span.SequenceEqual(Fixture(shared: true))
            || Apply(Fixture(observesX: true)) is not null || Apply(Fixture(observesRetained: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object? Apply(ReadOnlyMemory<byte> code) => Call(
            compiler,
            "ShareClosedShortcutFallback",
            code,
            7
        );
    }

    private static byte[] Fixture(bool shared = false, bool observesX = false, bool observesRetained = false)
    {
        byte[] entry = shared
            ? [ExactNotEqualTest, F(3), X(0), A(2)]
            : [ExactEqualTest, F(3), X(0), A(2), Move, A(3), X(0), Jump, F(6), Label, U(3)];
        byte[] join = shared
            ? [Label, U(3), Move, A(3), Y(0), Label, U(4), InitYRegisters, ExtendedListOperand, U(1), Y(1)]
            : [Move, A(3), X(0), Jump, F(5), Label, U(4), Move, Y(0), X(0), Label, U(5), Label, U(6),
                Move, X(0), Y(1)];
        byte[] extra = observesX ? [Move, X(0), X(1)] : observesRetained ? [Move, Y(0), X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            .. entry, ExternalCall, U(1), U(0), Move, X(0), Y(0),
            ExternalCall, U(0), U(1), IsNil, F(4), X(0), ExternalCall, U(0), U(2), IsNil, F(4), X(0),
            .. join, .. extra, Move, Y(shared ? 0 : 1), X(0), Deallocate, U(2), Return, EndCode
        ];
    }
}
