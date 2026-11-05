namespace Erlang;

internal sealed class PrimBufferDataResource
{
    internal object Gate { get; } = new();
    internal Queue<BitString> Segments { get; } = new();
    internal int HeadOffset { get; set; }
    internal long Size { get; set; }
    internal int Locked;
    private readonly byte[] accumulator = new byte[PrimBufferBuiltinNames.WriteAccumulatorBytes];
    private int accumulated;

    internal void FlushAccumulator()
    {
        if (accumulated == 0)
        {
            return;
        }
        Segments.Enqueue(BitString.FromOwnedBytes(accumulator.AsSpan(0, accumulated).ToArray()));
        accumulated = 0;
    }

    internal void Write(IReadOnlyList<BitString> values)
    {
        long bytes = values.Sum(value => (long)value.BitLength / BitStringLayout.BitsPerByte);
        if (accumulated + bytes >= PrimBufferBuiltinNames.WriteAccumulatorBytes)
        {
            FlushAccumulator();
            if (bytes >= PrimBufferBuiltinNames.WriteAccumulatorBytes / 2)
            {
                foreach (BitString value in values)
                {
                    if (value.BitLength != 0)
                    {
                        Segments.Enqueue(value);
                    }
                }
                Size += bytes;

                return;
            }
        }
        foreach (BitString value in values)
        {
            value.ByteSpan.CopyTo(accumulator.AsSpan(accumulated));
            accumulated += value.BitLength / BitStringLayout.BitsPerByte;
        }
        Size += bytes;
    }
}
