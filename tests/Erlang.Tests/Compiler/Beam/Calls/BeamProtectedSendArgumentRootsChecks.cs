using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProtectedSendArgumentRootsChecks
{
    internal const string TestName = "compiler/beam/clears-saved-roots-before-closed-protected-send";
    private const string Module = "beam_protected_send_argument_roots_fixture";
    private const string WrongLayout = "The outgoing registers must retain each released root and preserve the exceptional return.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        if (Rewrite(compiler, Fixture()) is not { } actual || !actual.SequenceEqual(Fixture(clear: true))
            || Rewrite(compiler, actual) is not null || Rewrite(compiler, Fixture(handlerReadsRoot: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] Fixture(bool clear = false, bool handlerReadsRoot = false)
    {
        byte[] roots = clear ? [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)] : [];
        byte recover = handlerReadsRoot ? Y(1) : A(1);

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(3), U(2), Move, X(0), Y(0), Move, X(1), Y(1), TryEnter, Y(2), F(3),
            TestHeap, U(3), U(0), PutTuple, X(1), ExtendedListOperand, U(2), A(1), Y(1),
            Move, Y(0), X(0), .. roots, Line, U(1), Send, TryEnd, Y(2), Deallocate, U(3), Return,
            Label, U(3), TryCase, Y(2), Move, recover, X(0), Deallocate, U(3), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            "ClearProtectedSendArgumentRoots",
            code,
            4
        );
}
