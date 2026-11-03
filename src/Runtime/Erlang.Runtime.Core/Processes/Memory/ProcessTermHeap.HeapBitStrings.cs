namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal bool CanOwnHeapBitString(BitString source)
        => source.ResourceBacking577.NativeStorage524 is null
            && !source.HasReferenceStorage425
            && source.BitLength is >= 0 and <= HeapBitStringLayout.MaximumBytes * HeapBitStringLayout.BitsPerByte
            && !runtime.IsSharingLiteral425(source);

    internal BitString CreateHeapBitString(BitString source, ProcessTermConstruction514 construction)
    {
        if (!Monitor.IsEntered(Gate) || !construction.Contains(source) || !CanOwnHeapBitString(source))
            throw new InvalidOperationException(HeapBitStringLayout.Invalid);
        int bits = source.BitLength;
        byte[] bytes = source.ToArray();
        var words = new nuint[HeapBitStringLayout.SizeWords + HeapBitStringLayout.DataWords(bits)];
        words[0] = (nuint)bits;
        for (int index = 0; index < bytes.Length; index++)
        {
            int shift = (BitConverter.IsLittleEndian ? index % IntPtr.Size : IntPtr.Size - 1 - index % IntPtr.Size)
                * HeapBitStringLayout.BitsPerByte;
            words[HeapBitStringLayout.SizeWords + index / IntPtr.Size] |= (nuint)bytes[index] << shift;
        }
        ProcessTermSequence506 block = AllocateEncoded526([], [HeapBitStringLayout.Header(bits)], words);
        try
        {
            var result = new BitString(block);
            construction.Publish(result);

            return result;
        }
        catch
        {
            ReleaseRegisteredHeader526(block);
            throw;
        }
    }

    internal int ReadHeapBitLength(ProcessTermSequence506 block)
    {
        if (LiteralDomain800 is { IsSealed800: true } domain)
        {
            using var reader = domain.EnterRead800(block);

            return RequireHeapBitLength(block);
        }
        lock (Gate) return RequireHeapBitLength(block);
    }

    private int RequireHeapBitLength(ProcessTermSequence506 block)
    {
        if (!ReferenceEquals(block.Owner, this) || !blocks.TryGetValue(block.Identity, out var current)
            || !ReferenceEquals(block, current))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        if (block.Count != 0 || block.HeaderWords526 != HeapBitStringLayout.HeaderWords
            || block.RawPayloadWords556 < HeapBitStringLayout.SizeWords)
            throw new InvalidOperationException(HeapBitStringLayout.Invalid);
        nuint raw = block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + HeapBitStringLayout.HeaderWords);
        if (raw > HeapBitStringLayout.MaximumBytes * HeapBitStringLayout.BitsPerByte)
            throw new InvalidOperationException(HeapBitStringLayout.Invalid);
        int bits = checked((int)raw);
        if (block.RawPayloadWords556 != HeapBitStringLayout.SizeWords + HeapBitStringLayout.DataWords(bits)
            || block.Slab.Cells.ReadHeader526(block.AllocationOffset526) != HeapBitStringLayout.Header(bits))
            throw new InvalidOperationException(HeapBitStringLayout.Invalid);

        return bits;
    }

    internal byte ReadHeapBitByte(ProcessTermSequence506 block, int index)
    {
        if (LiteralDomain800 is { IsSealed800: true } domain)
        {
            using var reader = domain.EnterRead800(block);

            return ReadLiteralHeapByteCore800(block, index);
        }
        lock (Gate) return ReadLiteralHeapByteCore800(block, index);
    }

    private byte ReadLiteralHeapByteCore800(ProcessTermSequence506 block, int index)
    {
        int bits = RequireHeapBitLength(block);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (bits + HeapBitStringLayout.BitsPerByte - 1) / HeapBitStringLayout.BitsPerByte);

        return ReadHeapBitByteCore(block, index);
    }

    private static byte ReadHeapBitByteCore(ProcessTermSequence506 block, int index)
    {
        nuint word = block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + HeapBitStringLayout.HeaderWords
            + HeapBitStringLayout.SizeWords + index / IntPtr.Size);
        int shift = (BitConverter.IsLittleEndian ? index % IntPtr.Size : IntPtr.Size - 1 - index % IntPtr.Size)
            * HeapBitStringLayout.BitsPerByte;

        return (byte)(word >> shift);
    }

    internal byte[] CopyHeapBitBytes(ProcessTermSequence506 block)
    {
        if (LiteralDomain800 is { IsSealed800: true } domain)
        {
            using var reader = domain.EnterRead800(block);

            return CopyLiteralHeapBytesCore800(block);
        }
        lock (Gate) return CopyLiteralHeapBytesCore800(block);
    }

    private byte[] CopyLiteralHeapBytesCore800(ProcessTermSequence506 block)
    {
        int bits = RequireHeapBitLength(block);
        var result = new byte[(bits + HeapBitStringLayout.BitsPerByte - 1) / HeapBitStringLayout.BitsPerByte];
        for (int index = 0; index < result.Length; index++)
            result[index] = ReadHeapBitByteCore(block, index);

        return result;
    }
}
