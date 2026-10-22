using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamPureMailboxControlBytes
{
    internal static byte[] Image(string importFunction = "node")
        => BeamOpcodeEdgeBytes.Image(
            [
            .. Start(
                1,
                2,
                2,
                0
            ),12,U(0),U(0),
            1,U(3),23,F(6),X(0),
            57,F(5),X(0),58,F(5),X(0),U(2),
            66,X(0),U(0),X(1),66,X(0),U(1),X(2),
            10,F(5),U(0),X(1),X(3),
            40,F(5),X(2),I(2),39,F(5),X(2),I(5),
            21,18,U(0),19,
            1,U(5),24,F(3),
            1,U(6),26,F(3),I(0),22,64,A(7),X(0),18,U(0),19
        ],
            labels: 7,
            arity: 0,
            importFunction: importFunction,
            importArity: 1
        );
}
