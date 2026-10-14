using System.Buffers.Binary;

namespace Erlang;

internal sealed class BeamCursor(ReadOnlyMemory<byte> bytes)
{
    public int Position { get; private set; }
    public int Remaining => bytes.Length - Position;

    public ReadOnlyMemory<byte> Read(int count)
    {
        if (count < 0 || count > Remaining)
        {
            throw new InvalidDataException(BeamDiagnostics.TruncatedField);
        }
        ReadOnlyMemory<byte> result = bytes.Slice(Position, count);
        Position += count;

        return result;
    }

    public byte ReadByte() => Read(1).Span[0];

    public int ReadInt32() => BinaryPrimitives.ReadInt32BigEndian(Read(BeamCursorWire.SignedIntegerFieldBytes).Span);
}
