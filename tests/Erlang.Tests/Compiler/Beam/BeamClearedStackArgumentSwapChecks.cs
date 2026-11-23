using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClearedStackArgumentSwapChecks
{
    internal const string TestName = "compiler/beam/swaps-call-arguments-with-a-cleared-stack-temporary";
    private const string WrongLayout = "Only a cleared stack temporary may be removed from outgoing argument exchange.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_cleared_stack_argument_swap");
        if (Apply(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(swapped: true))
            || Apply(changed) is not null || Apply(Fixture(clear: false)) is not null
            || Apply(Fixture(overwriteOperand: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "SwapClearedStackCallArguments",
            code,
            3
        );
    }

    private static byte[] Fixture(bool swapped = false, bool clear = true, bool overwriteOperand = false)
    {
        byte[] setup = swapped
            ? [Move, A(1), X(3), Move, I(0), X(1), Swap, X(0), X(2)]
            : [Move, X(0), Y(0), Move, X(2), X(0), Move, Y(0), X(2),
                Move, I(0), X(overwriteOperand ? 0 : 1), Move, A(1), X(3)];
        byte[] cleared = clear ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            Allocate, U(1), U(4), .. setup, .. cleared, Line, U(1), LocalCall, U(4), F(2),
            Deallocate, U(1), Return, EndCode
        ];
    }
}
