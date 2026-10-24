using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamHandlerRouting396Names;

namespace Erlang.Tests;

internal static class BeamHandlerRouting396Bytes
{
    internal static byte[] Image(
        bool returning = false,
        bool oneReturningArm = false,
        bool badRoot = false,
        bool directJump = false,
        bool wrongSlot = false,
        bool tuplePrefix = false
    )
    {
        byte[] caller = [.. Start(
            1,
            2,
            2,
            1
        ), Allocate, U(1), U(1), Try, Y(0), F(3),
            .. (directJump ? new byte[] { Jump, F(3) } : new byte[] { Call, U(1), F(5) }),
            Label, U(3), TryCase, Y(wrongSlot ? 1 : 0), Move, A(7), X(0), Deallocate, U(1), Return];
        byte[] end = returning ? [Deallocate, U(0), Return]
            : oneReturningArm ? [Select, X(0), F(9), List, U(2), A(7), F(8),
                Label, U(8), Deallocate, U(0), Return,
                Label, U(9), ExternalLast, U(1), U(0), U(0)]
            : [ExternalLast, U(1), U(0), U(badRoot ? 1 : 0)];
        byte[] middle = tuplePrefix ? [HeapTest, U(3), U(1), PutTuple, X(0), List, U(2), A(7), X(0), ExternalOnly, U(1), U(0)]
            : [Allocate, U(badRoot ? 1 : 0), U(1), Call, U(1), F(7), .. end];

        return BeamOpcodeEdgeBytes.Image(
            [
            .. caller, .. Start(
                4,
                5,
                3,
                1
            ), .. middle,
            .. Start(
                6,
                7,
                4,
                1
            ), Move, A(8), X(0), Return
        ],
            labels: 10,
            functions: 3,
            arity: 1,
            importFunction: Error
        );
    }
}
