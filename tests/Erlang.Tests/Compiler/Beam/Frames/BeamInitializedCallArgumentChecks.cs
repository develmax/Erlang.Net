using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamInitializedCallArgumentChecks
{
    internal const string TestName = "compiler/beam/forwards-an-initialized-call-argument";
    private const string Pass = "ForwardInitializedCallArgument";
    private const string WrongCode = "Only the temporary saved argument may become a direct copy before the call.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_initialized_call_argument");
        if (Rewrite(compiler, Fixture()) is not { } actual
            || !actual.SequenceEqual(Fixture(direct: true))
            || Rewrite(compiler, actual) is not null
            || Rewrite(compiler, Fixture(laterRead: true)) is not null
            || Rewrite(compiler, Fixture(clear: false)) is not null)
        {
            throw new InvalidOperationException(WrongCode);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, byte[] code) =>
        (byte[]?)Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(code),
            5
        );

    private static byte[] Fixture(bool direct = false, bool laterRead = false, bool clear = true)
    {
        byte[] initialization = direct ? [U(2), Y(0), Y(1)] : [U(1), Y(0)];
        byte[] argument = direct ? [Move, X(0), X(1)] : [Move, X(0), Y(1), Move, Y(1), X(1)];
        byte[] use = laterRead ? [Move, Y(1), X(2)] : [];
        byte[] retire = direct || !clear ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ), Allocate, U(3), U(2), InitYRegisters, ExtendedListOperand, .. initialization,
            Move, X(1), Y(2), .. argument, Move, A(0), X(2), Move, A(1), X(0), .. use, .. retire,
            Line, U(1), ExternalCall, U(3), U(0), Move, Y(2), X(0), Deallocate, U(3), Return, EndCode
        ];
    }
}
