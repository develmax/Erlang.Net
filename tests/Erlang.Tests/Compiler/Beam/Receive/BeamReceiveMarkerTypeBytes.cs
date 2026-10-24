using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;

namespace Erlang.Tests;

internal static class BeamReceiveMarkerTypeBytes
{
    internal static byte[] Image(byte[] body, int arity = 0, int labels = 3)
    {
        // Synthetic wire image; no original OTP execution/compatibility credit.
        byte[] image = BeamOpcodeEdgeBytes.Image([.. Start(
            1,
            2,
            2,
            arity
        ), .. body], labels: labels, arity: arity);
        for (int cursor = 12; cursor < image.Length;)
        {
            string chunk = Encoding.ASCII.GetString(image, cursor, 4);
            int length = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(cursor + 4));
            if (chunk == Code)
            {
                byte[] code = image.AsSpan(cursor + 8, length).ToArray();
                BinaryPrimitives.WriteInt32BigEndian(code.AsSpan(MaximumOpcodeOffset), Use);

                return BeamMetadataModeledBytes.WithChunk(image, Code, code);
            }
            cursor += 8 + ((length + 3) & ~3);
        }
        throw new InvalidDataException(Failure);
    }

    internal static byte[] Receive(bool staleTimeout = false, bool rejectedScratch = false)
    {
        if (rejectedScratch)
            return Image(
                [
                Reserve, X(0), Allocate, U(0), U(1),
                Label, U(3), LoopReceive, F(6), X(0),
                Move, X(0), X(1), EqualOpcode, F(5), X(1), A(8),
                RemoveMessage, Deallocate, U(0), Return,
                Label, U(5), LoopReceiveEnd, F(3),
                Label, U(6), WaitTimeout, F(3), I(0), TimeoutOpcode,
                Move, X(1), X(0), Deallocate, U(0), Return
            ],
                labels: 7
            );

        return Image(
            [
            Allocate, U(3), U(2), Move, X(0), Y(0), Move, X(1), Y(1),
            Reserve, Y(2), Bind, Y(2), Y(0),
            Move, Y(1), X(0), CallFun, U(0), Use, Y(0),
            Label, U(3), LoopReceive, F(6), X(0),
            EqualOpcode, F(5), X(0), A(8), RemoveMessage, Clear, Y(0),
            Deallocate, U(3), Return,
            Label, U(5), LoopReceiveEnd, F(3),
            Label, U(6), WaitTimeout, F(3), I(0), TimeoutOpcode,
            .. (staleTimeout ? new byte[] { Move, X(1), X(0) } : new byte[] { Move, A(7), X(0) }),
            Clear, Y(0), Deallocate, U(3), Return
        ],
            arity: 2,
            labels: 7
        );
    }

    internal static byte[] ClearMatcher(bool beforeRejection)
        => Image(
            [
            Allocate, U(3), U(2), Move, X(0), Y(0), Move, X(1), Y(1),
            Reserve, Y(2), Bind, Y(2), Y(0), Move, Y(1), X(0), CallFun, U(0), Use, Y(0),
            Label, U(3), LoopReceive, F(6), X(0),
            .. (beforeRejection ? new byte[] { Clear, Y(0) } : Array.Empty<byte>()),
            TaggedTuple, F(5), X(0), U(2), A(8), HeapTest, U(0), U(1),
            .. (beforeRejection ? Array.Empty<byte>() : new byte[] { Clear, Y(0) }),
            TupleElement, X(0), U(1), X(0), RemoveMessage, Deallocate, U(3), Return,
            Label, U(5), LoopReceiveEnd, F(3),
            Label, U(6), WaitTimeout, F(3), I(0), TimeoutOpcode,
            Move, A(7), X(0), Clear, Y(0), Deallocate, U(3), Return
        ],
            arity: 2,
            labels: 7
        );
}
