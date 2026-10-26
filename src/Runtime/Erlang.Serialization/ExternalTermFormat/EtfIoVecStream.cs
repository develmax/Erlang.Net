namespace Erlang;

internal sealed class EtfIoVecStream : Stream
{
    private readonly MemoryStream header = new();
    private readonly List<Segment> segments = new();
    private long headerStart;
    private long length;
    private long position;
    private bool finished;
    public override bool CanRead => false;
    public override bool CanWrite => !finished;
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

    public void Append(BitString binary)
    {
        EnsureWritable();
        if (position != length || !binary.IsBinary)
            throw new InvalidOperationException();
        FlushHeader();
        segments.Add(new Segment(length, null, binary));
        length += binary.ByteLength;
        position = length;
        headerStart = length;
    }

    public IReadOnlyList<BitString> Finish()
    {
        EnsureWritable();
        FlushHeader();
        finished = true;

        return Array.AsReadOnly(segments.Select(segment => segment.Binary ?? BitString.FromOwnedBytes(segment.Header!)).ToArray());
    }

    public override void Write(ReadOnlySpan<byte> bytes)
    {
        EnsureWritable();
        if (position == length)
        {
            header.Position = header.Length;
            header.Write(bytes);
            length += bytes.Length;
            position = length;

            return;
        }
        if (position >= headerStart)
        {
            if (bytes.Length > length - position)
                throw new InvalidOperationException();
            header.Position = position - headerStart;
            header.Write(bytes);
        }
        else
        {
            var segment = segments.Last(value => value.Start <= position);
            if (segment.Header is null || bytes.Length > segment.Header.Length - (position - segment.Start))
                throw new InvalidOperationException();
            bytes.CopyTo(segment.Header.AsSpan(checked((int)(position - segment.Start))));
        }
        position += bytes.Length;
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void WriteByte(byte value)
    {
        Span<byte> bytes = stackalloc byte[1];
        bytes[0] = value;
        Write(bytes);
    }

    public override void Flush() => FlushHeader();

    private void EnsureWritable()
    {
        if (finished)
            throw new InvalidOperationException();
    }

    private void FlushHeader()
    {
        if (header.Length == 0)
            return;
        segments.Add(new Segment(headerStart, header.ToArray(), null));
        header.SetLength(0);
        headerStart = length;
    }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            header.Dispose();
        base.Dispose(disposing);
    }

    private sealed record Segment(long Start, byte[]? Header, BitString? Binary);
}
