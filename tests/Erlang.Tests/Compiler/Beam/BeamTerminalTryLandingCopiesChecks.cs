using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalTryLandingCopiesChecks
{
    internal const string TestName = "compiler/beam/removes-dead-terminal-try-copies-in-sibling-handlers";
    private const string Module = "beam_terminal_try_landing_copies_probe";
    private const string WrongLayout = "Removing dead exception copies must preserve values read by a sibling handler.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        Verify(compiler, liveSecondReason: false);
        Verify(compiler, liveSecondReason: true);

        return Task.CompletedTask;
    }

    private static void Verify(BeamModuleCompiler compiler, bool liveSecondReason)
    {
        byte[] original = Fixture(compacted: false, liveSecondReason: liveSecondReason);
        byte[] expected = Fixture(compacted: true, liveSecondReason: liveSecondReason);
        if (Rewrite(compiler, original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(compiler, actual) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static byte[] Fixture(bool compacted, bool liveSecondReason)
    {
        byte[] copies = [Move, X(0), Y(1), Move, X(1), Y(2), Move, X(2), X(0), Move, X(0), Y(3)];
        byte[] first = compacted ? [] : copies;
        byte[] second = compacted && !liveSecondReason ? [] : copies;

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsLess, F(4), X(0), A(1),
            .. Arm(3), Label, U(3), TryCase, Y(0), .. first, Move, A(1), X(0),
            Deallocate, U(4), Return,
            Label, U(4), .. Arm(5), Label, U(5), TryCase, Y(0), .. second,
            Move, liveSecondReason ? Y(2) : A(1), X(0), Deallocate, U(4), Return, EndCode
        ];

        static byte[] Arm(int handler) =>
        [
            Allocate, U(4), U(1), InitYRegisters, ExtendedListOperand, U(3), Y(1), Y(2), Y(3),
            TryEnter, Y(0), F(handler), ExternalCall, U(1), U(0),
            TryEnd, Y(0), Deallocate, U(4), Return
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "RemoveTerminalTryLandingCopies",
            code,
            6
        );
}
