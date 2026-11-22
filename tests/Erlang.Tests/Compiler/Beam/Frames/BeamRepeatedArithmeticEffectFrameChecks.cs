using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRepeatedArithmeticEffectFrameChecks
{
    internal const string TestName = "compiler/beam/places-repeated-arithmetic-before-its-effect-frame";
    private const string Unexpected = "Expected the same pre-frame BIF, owned inputs and final trim for both entry layouts.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("arithmetic_effect_frame_fixture");
        int arithmetic = (int)Call(
            compiler,
            "Import",
            "erlang",
            "-",
            2
        )!;
        int effect = (int)Call(
            compiler,
            "Import",
            "effects",
            "mark",
            3
        )!;
        foreach (bool late in new[] { false, true })
        {
            if (Rewrite(Fixture(late)) is not byte[] actual || !actual.SequenceEqual(Fixture(late: false, written: true))
                || Rewrite(actual) is not null || Rewrite(Fixture(late, variesArithmetic: true)) is not null)
            {
                throw new InvalidOperationException(Unexpected);
            }
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => Call(
            compiler,
            "ScheduleRepeatedArithmeticEffectFrame",
            new ReadOnlyMemory<byte>(code),
            5
        );

        byte[] Fixture(bool late, bool written = false, bool variesArithmetic = false)
        {
            byte[] soon = [Line, U(1), GcBifTwo, F(0), U(2), U(arithmetic), X(1), I(1), X(2)];
            byte[] saves = written ? [Move, X(1), Y(0), Move, X(0), Y(1)] : [Move, X(0), Y(0), Move, X(1), Y(1)];
            byte[] outbound = late
                ? [Line, U(1), GcBifTwo, F(0), U(0), U(arithmetic), Y(1), I(1), X(1), Move, I(0), X(2), Move, Y(0), X(0)]
                : [Move, X(2), X(1), Move, I(0), X(2)];
            byte[] trimmed = written ? [TrimFrame, U(2), U(0)] : [];
            int retained = written ? 0 : 1;
            int receiver = 1 - retained;

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    3
                ),
                .. (late ? [] : soon), Allocate, U(2), U(late ? 2 : 3), .. saves, .. outbound,
                ExternalCall, U(3), U(effect), Line, U(2), GcBifTwo, F(0), U(0),
                U(arithmetic), Y(retained), I(variesArithmetic ? 2 : 1), X(1),
                Move, I(1), X(2), Move, Y(receiver), X(0), .. trimmed,
                ExternalCall, U(3), U(effect), Move, A(1), X(0), Deallocate, U(written ? 0 : 2), Return, EndCode
            ];
        }
    }
}
