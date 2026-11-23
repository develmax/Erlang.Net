using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalEffectCallTrimChecks
{
    internal const string TestName = "compiler/beam/trims-the-final-retained-effect-call-slot";
    private const string Pass = "TrimSingleSlotAfterFinalCallSetup";
    private const string Mismatch = "Expected the final ordinary call to use its outgoing argument root after the last frame trim.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("terminal_effect_call_trim_fixture");
        if (Rewrite(Fixture()) is not byte[] trimmed || !trimmed.SequenceEqual(Fixture(trimmed: true))
            || Rewrite(trimmed) is not null || Rewrite(Fixture(sharedEntry: true)) is not null
            || Rewrite(Fixture(returnSaved: true)) is not null)
        {
            throw new InvalidOperationException(Mismatch);
        }

        return Task.CompletedTask;

        byte[]? Rewrite(byte[] bytes) => (byte[]?)Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(bytes),
            4
        );
    }

    private static byte[] Fixture(bool trimmed = false, bool sharedEntry = false, bool returnSaved = false)
    {
        byte[] released = trimmed ? [TrimFrame, U(1), U(0)] : [];
        byte[] marker = sharedEntry ? [Label, U(3)] : [];
        byte source = returnSaved ? Y(0) : A(1);

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2), Move, X(0), Y(0), Move, X(1), Y(1),
            Move, Y(0), X(0), TrimFrame, U(1), U(1), LocalCall, U(1), F(2),
            .. marker, Move, Y(0), X(0), .. released, Line, U(1), LocalCall, U(1), F(2),
            Move, source, X(0), Deallocate, U(trimmed ? 0 : 1), Return, EndCode
        ];
    }
}
