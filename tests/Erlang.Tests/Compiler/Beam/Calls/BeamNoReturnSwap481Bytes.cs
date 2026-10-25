using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamClosedWait412Names;

namespace Erlang.Tests;

internal static class BeamNoReturnSwap481Bytes
{
    internal static byte[] Image(bool returning = false, bool uninitialized = false, bool unknown = false)
    {
        byte[] finish = returning ? [Return] : [ExternalTail, U(1), U(0)];

        return BeamOpcodeEdgeBytes.Image(
            [.. Start(
                RunInfo,
                RunEntry,
                RunAtom,
                1
            ), Allocate, U(1), U(1), Try, Y(0), F(Handler), Call, U(1), F(MiddleEntry),
            Label, U(Handler), TryCase, Y(0), Move, A(MarkerAtom), X(0), Deallocate, U(1), Return,
            .. Start(
                MiddleInfo,
                MiddleEntry,
                MiddleAtom,
                1
            ), Move, I(7), X(1), BeamNoReturnSwap481Names.Swap, X(0), X(uninitialized ? 2 : 1), .. finish],
            labels: 6,
            functions: 2,
            importFunction: unknown ? LengthFunction : ErrorFunction
        );
    }
}
