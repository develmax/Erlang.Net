using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;

namespace Erlang.Tests;

internal static class BeamDelayProtocolBytes
{
    internal static byte[] Unprotected(bool uninitializedTimeout = false, bool readExpiredX = false)
        => BeamReceiveMarkerTypeBytes.Image(
            [
                Allocate, U(0), U(1), Label, U(3), WaitTimeout, F(3), X(uninitializedTimeout ? 1 : 0),
                TimeoutOpcode, Move, readExpiredX ? X(0) : A(7), X(0), Deallocate, U(0), Return
            ],
            arity: 1,
            labels: 4
        );

    internal static byte[] Image(
        bool invalidHandler = false,
        bool handlerTimeout = false,
        bool markerTimeout = false,
        bool uninitializedTimeout = false,
        bool wrongTarget = false,
        bool missingTimeout = false,
        bool xTimeout = false,
        bool readExpiredX = false
    )
        => BeamReceiveMarkerTypeBytes.Image(
            [
                Allocate, U(2), U(1), Move, X(0), Y(0),
                .. (markerTimeout ? new byte[] { Reserve, Y(0) } : Array.Empty<byte>()),
                Try, Y(1), F(7),
                Label, U(3), WaitTimeout, F(wrongTarget ? 7 : 3),
                uninitializedTimeout ? X(3) : xTimeout ? X(0) : Y(handlerTimeout ? 1 : 0),
                .. (missingTimeout ? Array.Empty<byte>() : new byte[] { TimeoutOpcode }),
                TryEnd, Y(1), Move, readExpiredX ? X(0) : A(7), X(0), Deallocate, U(2), Return,
                Label, U(7), TryCase, Y(1), Move, invalidHandler ? X(3) : A(7), X(0), Deallocate, U(2), Return
            ],
            arity: 1,
            labels: 8
        );
}
