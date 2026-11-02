namespace Erlang;

public sealed partial class BitString
{
    private readonly IOwnedResourceBitsStorage577? ownedResourceBits577;
    private readonly BitString? resourceSliceOrigin577;
    internal IOwnedResourceBitsStorage577? OwnedStorage577 => ownedResourceBits577;
    internal BitString? ResourceSliceOrigin577 => resourceSliceOrigin577;
    private long LogicalBitOffset577 => ownedResourceBits577?.ReadResourceBits577().BitOffset ?? bitOffset;

    // Backing is range/custody authority only; owned size/offset always read cells.
    internal BitString(BinaryBacking custody, IOwnedResourceBitsStorage577 storage)
    {
        backing = custody;
        ownedResourceBits577 = storage;
    }

    private void ValidateOwnedBits577()
    {
        _ = ownedHeapBits?.ReadHeapBitLength();
        _ = ownedResourceBits577?.ReadResourceBits577();
    }

    internal BinaryBacking ResourceBacking577 => backing;

    internal ResourceBitsRead577 CaptureResourceBits577()
        => ownedHeapBits is not null ? new(0, ownedHeapBits.ReadHeapBitLength())
            : ownedResourceBits577?.ReadResourceBits577() ?? new(bitOffset, portableBitLength577);

    internal BitString DetachResourceView577()
    {
        ResourceBitsRead577 read = CaptureResourceBits577();

        return ownedHeapBits is not null ? new BitString(ownedHeapBits.CopyHeapBytes(), read.BitLength)
            : new BitString(backing, read.BitOffset, read.BitLength);
    }

    private ReadOnlySpan<byte> ReadOwnedByteSpan577()
    {
        ResourceBitsRead577 read = ownedResourceBits577!.ReadResourceBits577();
        ReadOnlySpan<byte> source = backing.ReadSpan524;
        int bytes = checked((read.BitLength + BitStringLayout.BitsPerByte - 1) / BitStringLayout.BitsPerByte);
        if (read.BitOffset % BitStringLayout.BitsPerByte == 0 && read.BitLength % BitStringLayout.BitsPerByte == 0)
            return source.Slice(checked((int)(read.BitOffset / BitStringLayout.BitsPerByte)), bytes);
        var result = new byte[bytes];
        for (int index = 0; index < read.BitLength; index++)
        {
            long sourceBit = checked(read.BitOffset + index);
            int bit = (source[checked((int)(sourceBit / BitStringLayout.BitsPerByte))]
                >> (BitStringStorage.HighBit - (int)(sourceBit % BitStringLayout.BitsPerByte))) & 1;
            result[index / BitStringLayout.BitsPerByte] |= (byte)(bit << (BitStringStorage.HighBit - index % BitStringLayout.BitsPerByte));
        }

        ObservedPayloadStorage.Binary(result);

        return result;
    }

    private BitString SliceOwnedResourceBits577(int offset, int count)
    {
        ResourceBitsRead577 read = ownedResourceBits577!.ReadResourceBits577();
        var result = new BitString(
            backing,
            checked(read.BitOffset + offset),
            count,
            sliceOrigin577: this
        );
        if (count > BitStringStorage.HeapBytes * BitStringLayout.BitsPerByte)
            return result;

        return new BitString(result.ByteSpan, count);
    }
}
