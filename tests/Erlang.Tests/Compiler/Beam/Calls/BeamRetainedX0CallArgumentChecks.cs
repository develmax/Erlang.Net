using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRetainedX0CallArgumentChecks
{
    internal const string TestName = "compiler/beam/keeps-saved-call-result-in-x0-through-literal-arguments";
    private const string Module = "beam_retained_x0_call_fixture";
    private const string WrongLayout = "The outgoing X0 must still hold the saved call result across literal setup.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        foreach (bool protectedCall in new[] { false, true })
        {
            if (Rewrite(compiler, Fixture(protectedCall)) is not { } actual
                || !actual.SequenceEqual(Fixture(protectedCall, forwarded: true))
                || Rewrite(compiler, actual) is not null
                || Rewrite(compiler, Fixture(protectedCall, overwrite: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool protectedCall, bool forwarded = false, bool overwrite = false)
    {
        byte width = U(protectedCall ? 2 : 1);
        byte[] install = protectedCall ? [TryEnter, Y(1), F(3)] : [];
        byte[] finish = protectedCall ? [TryEnd, Y(1)] : [];
        byte[] raised = protectedCall
            ? [Label, U(3), TryCase, Y(1), Move, A(1), X(0), Deallocate, width, Return]
            : [];
        byte[] extra = overwrite ? [Move, A(1), X(0)] : [];
        byte[] restore = forwarded ? [] : [Move, Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Allocate, width, U(0), InitYRegisters, ExtendedListOperand, U(1), Y(0), .. install,
            Line, U(1), ExternalCall, U(0), U(0), Move, X(0), Y(0),
            Move, A(1), X(1), Line, U(2), Move, I(2), X(2), .. extra, .. restore,
            Line, U(3), ExternalCall, U(3), U(1), Move, Y(0), X(0), .. finish,
            Deallocate, width, Return, .. raised, EndCode
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "ForwardLiveCallResultToArgument",
            code,
            4
        );
}
