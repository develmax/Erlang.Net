using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamCallbackPlan399Names;

namespace Erlang.Tests;

internal static class BeamCallbackPlan399Bytes
{
    internal static byte[] Image(
        int callArity = 1,
        int atom = 6,
        int calleeY = 1
    )
        => BeamOpcodeEdgeBytes.Image(
            [.. Start(
                1,
                2,
                2,
                2
            ), 12, U(2), U(2), 64, X(1), Y(0), 64, X(0), Y(1),
             1, U(3), 23, F(6), X(0), 64, Y(calleeY), X(1), 75, U(callArity),
             44, F(5), X(0), A(atom), 21, 18, U(2), 19,
             1, U(5), 24, F(3), 1, U(6), 26, F(3), Y(0), 22, 64, A(8), X(0), 18, U(2), 19],
            labels: 7,
            arity: 2,
            importFunction: NoMatchAtom,
            importArity: 0
        );
}
