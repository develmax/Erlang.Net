using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;

namespace Erlang.Tests;

internal static class BeamProtectedReceiveProtocolBytes
{
    internal static byte[] Timed(
        bool invalidHandler = false,
        bool handlerTimeout = false,
        bool scratchTimeout = false,
        bool handlerMatcher = false,
        bool markerTimeout = false
    )
        => BeamReceiveMarkerTypeBytes.Image(
            [
            Allocate, U(2), U(1), Move, X(0), Y(0),
            .. (markerTimeout ? new byte[] { Reserve, Y(0) } : Array.Empty<byte>()),
            Try, Y(1), F(7),
            Label, U(3), LoopReceive, F(6), X(0),
            .. (handlerMatcher ? new byte[] { Move, Y(1), X(1) } : Array.Empty<byte>()),
            EqualOpcode, F(5), X(0), A(8),
            RemoveMessage, TryEnd, Y(1), Deallocate, U(2), Return,
            Label, U(5), LoopReceiveEnd, F(3),
            Label, U(6), WaitTimeout, F(3), scratchTimeout ? X(0) : Y(handlerTimeout ? 1 : 0),
            TimeoutOpcode, TryEnd, Y(1), Move, A(7), X(0), Deallocate, U(2), Return,
            Label, U(7), TryCase, Y(1), Move, invalidHandler ? X(3) : A(7), X(0),
            Deallocate, U(2), Return
        ],
            arity: 1,
            labels: 8
        );
}
