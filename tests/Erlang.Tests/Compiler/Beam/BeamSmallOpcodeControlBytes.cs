using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamSmallOpcodeControlNames;

namespace Erlang.Tests;

internal static class BeamSmallOpcodeControlBytes
{
    internal static byte[] Inequality(bool unreadY = false)
        => Image(
            [.. Start(
                1,
                2,
                2,
                2
            ),
            .. (unreadY ? new byte[] { Allocate, U(1), U(2) } : Array.Empty<byte>()),
            ArithmeticNotEqual, F(3), X(0), unreadY ? Y(0) : X(1),
            Move, A(7), X(0),
            .. (unreadY ? new byte[] { Deallocate, U(1) } : Array.Empty<byte>()), Return,
            Label, U(3), Move, A(8), X(0),
            .. (unreadY ? new byte[] { Deallocate, U(1) } : Array.Empty<byte>()), Return],
            labels: 4,
            arity: 2
        );

    internal static byte[] ReceiveInequality()
        => Image(
            [.. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(1), U(1), Move, X(0), Y(0),
            Label, U(3), LoopReceive, F(5), X(0),
            ArithmeticNotEqual, F(4), X(0), Y(0),
            RemoveMessage, Deallocate, U(1), Return,
            Label, U(4), LoopReceiveEnd, F(3),
            Label, U(5), WaitTimeout, F(3), I(0),
            ReceiveTimeoutOpcode, Move, A(8), X(0), Deallocate, U(1), Return],
            labels: 6,
            arity: 1
        );

    internal static byte[] Negate(
        bool initialize = true,
        bool nativeCall = false,
        bool fresh = false,
        bool inPlace = false,
        bool twice = false,
        bool wrongDestination = false
    )
    {
        int destination = inPlace ? 0 : 1;
        byte[] setup = initialize ? [FloatConvert, X(0), FloatRegister, U(0)] : [];
        byte[] call = nativeCall
            ? [Move, A(0), X(0), ExternalCall, U(1), U(0)] : [];
        byte[] renew = fresh ? [FloatConvert, I(3), FloatRegister, U(0)] : [];
        byte[] second = twice
            ? [FloatNegate, F(0), FloatRegister, U(destination), FloatRegister, U(destination)] : [];
        byte[] target = wrongDestination ? [X(0)] : [FloatRegister, U(destination)];

        return Image(
            [.. Start(
                1,
                2,
                2,
                1
            ),
            .. (nativeCall ? new byte[] { Allocate, U(0), U(1) } : Array.Empty<byte>()),
            .. setup, .. call, .. renew,
            FloatNegate, F(0), FloatRegister, U(0), .. target,
            .. second, HeapTest, U(2), U(0),
            FloatMove, FloatRegister, U(destination), X(0),
            .. (nativeCall ? new byte[] { Deallocate, U(0) } : Array.Empty<byte>()), Return],
            labels: 3,
            arity: 1
        );
    }
}
