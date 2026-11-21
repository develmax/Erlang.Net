using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLateCallKillOrderChecks
{
    internal const string TestName = "compiler/beam/orders-late-argument-zero-after-independent-frame-kills";
    private const string Module = "beam_late_call_kill_fixture";
    private const string Pass = "OrderCallArgumentAfterKills";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        byte[] copy = [Move, Y(0), X(0)];
        byte[] kill = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] doubleKill = [.. kill, InitYRegisters, ExtendedListOperand, U(1), Y(2)];
        byte[] external = [Line, U(5), ExternalCall, U(2), U(0)];
        byte[] local = [Line, U(5), LocalCall, U(1), F(5)];

        CheckOrder(compiler, [.. copy, .. kill, .. external], [.. kill, .. copy, .. external]);
        CheckOrder(compiler, [.. copy, .. doubleKill, .. local], [.. doubleKill, .. copy, .. local]);

        CheckUnchanged(
            compiler,
            [.. copy, .. kill, InitYRegisters, ExtendedListOperand, U(1), Y(0), .. external],
            BeamLateCallKillOrderDiagnostics.ClearedSource
        );
        CheckUnchanged(
            compiler,
            [Move, ExtendedTypedRegisterOperand, Y(0), U(1), X(0), .. kill, .. external],
            BeamLateCallKillOrderDiagnostics.TypedSource
        );
        CheckUnchanged(
            compiler,
            [Move, A(1), X(0), .. kill, .. external],
            BeamLateCallKillOrderDiagnostics.LiteralSource
        );
        CheckUnchanged(
            compiler,
            [.. copy, .. kill, TestHeap, U(2), U(2), .. external],
            BeamLateCallKillOrderDiagnostics.Collector
        );
        CheckUnchanged(
            compiler,
            [.. copy, .. kill, Line, U(5), ExternalCall, U(0), U(0)],
            BeamLateCallKillOrderDiagnostics.NoArgument
        );
        CheckUnchanged(
            compiler,
            [TryEnter, Y(2), F(3), .. copy, .. kill, .. external, TryEnd, Y(2)],
            BeamLateCallKillOrderDiagnostics.Protected
        );
        CheckUnchanged(
            compiler,
            [.. copy, .. kill, .. external, DebugLine, A(1), U(1), U(1), U(1)],
            BeamLateCallKillOrderDiagnostics.Debug
        );

        return Task.CompletedTask;
    }

    private static void CheckOrder(BeamModuleCompiler compiler, byte[] original, byte[] expected)
    {
        byte[] expectedCode = Fixture(expected);
        if (Rewrite(compiler, Fixture(original)) is not { } rewritten || !rewritten.SequenceEqual(expectedCode))
        {
            throw new InvalidOperationException(BeamLateCallKillOrderDiagnostics.Order);
        }

        if (Rewrite(compiler, expectedCode) is not null)
        {
            throw new InvalidOperationException(BeamLateCallKillOrderDiagnostics.AlreadyOrdered);
        }
    }

    private static void CheckUnchanged(BeamModuleCompiler compiler, byte[] body, string diagnostic)
    {
        if (Rewrite(compiler, Fixture(body)) is not null)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code)
        => (byte[]?)Call(
            compiler,
            Pass,
            code,
            6,
            true
        );

    private static byte[] Fixture(byte[] body)
    {
        byte[] handler = body[0] == TryEnter ? [TryCase, Y(2)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(3), U(3),
            Move, X(0), Y(0),
            Move, X(1), Y(1),
            Move, X(2), Y(2),
            .. body,
            Deallocate, U(3), Return,
            Label, U(3), .. handler,
            Deallocate, U(3), Return,
            .. Start(
                4,
                5,
                1,
                1
            ),
            Return,
            EndCode
        ];
    }
}
