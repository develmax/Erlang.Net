using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamGcBifCallArgumentChecks
{
    internal const string TestName = "compiler/beam/keeps-gc-bif-in-final-call-argument";
    private const string Pass = "ForwardGcBifCallArguments";
    private const string InvalidLayout = "The collecting BIF must keep its roots and failure while writing the final argument.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_gc_bif_call_argument");
        if (Rewrite(compiler, Fixture()) is not { } output || !output.SequenceEqual(Fixture(direct: true))
            || Rewrite(compiler, output) is not null
            || Rewrite(compiler, Fixture(failure: 3)) is not null
            || Rewrite(compiler, Fixture(replaceTemporary: false)) is not null
            || Rewrite(compiler, Fixture(intervening: [Move, X(0), X(2)])) is not null
            || Rewrite(compiler, Fixture(intervening: [Move, X(1), X(2)])) is not null
            || Rewrite(compiler, Fixture(intervening: [TestHeap, U(2), U(2)])) is not null)
        {
            throw new InvalidOperationException(InvalidLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, byte[] bytes) =>
        (byte[]?)Call(
            compiler,
            Pass,
            new ReadOnlyMemory<byte>(bytes),
            5
        );

    private static byte[] Fixture(
        bool direct = false,
        int failure = 0,
        bool replaceTemporary = true,
        byte[]? intervening = null
    )
    {
        byte[] reload = direct ? [] : [Move, X(0), X(1)];
        byte[] replace = replaceTemporary ? [Move, Y(0), X(0)] : [];
        byte[] reject = failure == 0 ? [] : [Label, U(3), Deallocate, U(1), Return];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ), Allocate, U(1), U(2), Move, X(1), Y(0),
            Line, U(1), GcBifTwo, F(failure), U(1), U(0), X(0), I(1), X(direct ? 1 : 0),
            .. reload, .. (intervening ?? []), .. replace, Line, U(2), ExternalCall, U(2), U(1),
            Deallocate, U(1), Return, .. reject, EndCode
        ];
    }
}
