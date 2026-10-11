namespace Erlang;

internal sealed class EtfSizeStream : Stream
{
    private long length;
    private long position;
    public override bool CanRead => false;
    public override bool CanWrite => true;
    public override bool CanSeek => true;
    public override long Length => length;
    public override long Position
    {
        get => position;
        set
        {
            if (value < 0 || value > length)
                throw new ArgumentOutOfRangeException(nameof(value));
            position = value;
        }
    }

    public void Add(int count)
    {
        position = checked(position + count);
        length = Math.Max(length, position);
    }

    public override void Write(ReadOnlySpan<byte> bytes) => Add(bytes.Length);

    public override void Write(byte[] buffer, int offset, int count) => Add(count);

    public override void WriteByte(byte value) => Add(1);

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = checked(offset + (origin switch
        {
            SeekOrigin.Begin => 0,
            SeekOrigin.Current => position,
            SeekOrigin.End => length,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        }));

        return position;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();
}
