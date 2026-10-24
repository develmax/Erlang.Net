using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;

namespace Erlang.Tests;

internal static class BeamClosureProtocolReceiveBytes
{
    internal static byte[] Image(bool rejectAfterWrite = false, bool overwriteHandler = false)
        => BeamReceiveMarkerTypeBytes.Image(
            [Allocate, U(3), U(0), Move, I(0), Y(0), Move, A(7), Y(1), Try, Y(2), F(7),
             Label, U(3), LoopReceive, F(6), X(0), EqualOpcode, F(5), X(0), A(8),
             Move, X(0), Y(overwriteHandler ? 2 : 1),
             .. (rejectAfterWrite ? new byte[] { EqualOpcode, F(5), X(0), A(8) } : Array.Empty<byte>()),
             RemoveMessage, TryEnd, Y(2), Move, Y(1), X(0), Deallocate, U(3), Return,
             Label, U(5), LoopReceiveEnd, F(3),
             Label, U(6), WaitTimeout, F(3), Y(0), TimeoutOpcode,
             TryEnd, Y(2), Move, A(7), X(0), Deallocate, U(3), Return,
             Label, U(7), TryCase, Y(2), Move, A(7), X(0), Deallocate, U(3), Return],
            labels: 8
        );
}
