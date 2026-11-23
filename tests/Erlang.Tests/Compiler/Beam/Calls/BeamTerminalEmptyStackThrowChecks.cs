using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalEmptyStackThrowChecks
{
    internal const string TestName = "compiler/beam/exits-the-frame-on-a-proven-empty-stack-throw";
    private const int SyntheticAtomCount = 3;
    private const string FrameError = "Only a frame-owned raise of a literal throw with an empty trace can be terminal.";

    internal static void Run()
    {
        byte[] original = Body(terminal: false);
        byte[] expected = Body(terminal: true);
        if (Rewrite(original) is not { } actual || !expected.SequenceEqual(actual)
            || Rewrite(expected) is not null
            || Rewrite(Body(terminal: false, trace: X(1))) is not null
            || Rewrite(Body(terminal: false, installCatch: true)) is not null
            || Rewrite(Body(terminal: false, reenter: true)) is not null)
        {
            throw new InvalidOperationException(FrameError);
        }
    }

    private static byte[] Body(
        bool terminal,
        byte trace = 2,
        bool installCatch = false,
        bool reenter = false
    )
    {
        byte[] handler = installCatch ? [CatchEnter, Y(0), F(3)] : [];
        byte[] call = terminal
            ? [TailExternalCallWithFrame, U(3), U(0), U(1)]
            : [ExternalCall, U(3), U(0), Jump, F(3)];
        byte[] leave = reenter ? [Jump, F(2)] : [Deallocate, U(1), Return];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ), Allocate, U(1), U(1), .. handler,
            Move, X(0), X(1), Move, trace, X(2), Move, A(2), X(0), Line, U(1), .. call,
            Label, U(3), .. leave, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            4,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            SyntheticAtomCount,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteFramedEmptyStackThrow",
            code,
            body,
            0,
            2
        );
    }
}
