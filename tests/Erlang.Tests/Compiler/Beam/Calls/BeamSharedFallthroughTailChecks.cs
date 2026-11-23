using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedFallthroughTailChecks
{
    internal const string TestName = "compiler/beam/folds-call-fallthrough-into-shared-return";
    private const string IncorrectCode = "The call or another predecessor lost its return path.";

    internal static Task Run()
    {
        byte[] original = Fixture(framed: true);
        byte[] expected = Fixture(framed: true, tail: true);
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected)
            || Rewrite(actual) is not null
            || Rewrite(Fixture(framed: false)) is not { } frameless
            || !frameless.SequenceEqual(Fixture(framed: false, tail: true))
            || Rewrite(Fixture(framed: true, subsequentWork: true)) is not null)
        {
            throw new InvalidOperationException(IncorrectCode);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool framed, bool tail = false, bool subsequentWork = false)
    {
        byte[] enter = framed ? [Allocate, U(1), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] call = !tail ? [ExternalCall, U(1), U(0)]
            : framed ? [TailExternalCallWithFrame, U(1), U(0), U(1)] : [TailExternalCall, U(1), U(0)];
        byte[] leave = framed ? [Deallocate, U(1)] : [];
        byte[] work = subsequentWork ? [Move, A(7), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            .. enter, ExactEqualTest, F(3), X(0), A(7),
            .. call, Label, U(3), .. work, .. leave, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        object header = New(
            "BeamCodeHeader",
            0,
            169,
            4,
            1,
            (ReadOnlyMemory<byte>)code
        );
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "FoldTerminalCalls",
            (ReadOnlyMemory<byte>)code,
            instructions
        );
    }
}
