using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed partial class BitString : Term
{
    private readonly BinaryBacking backing;
    private readonly object? writer;
    private ReadOnlySpan<byte> bytes => backing.ReadSpan524;
    private readonly long bitOffset;
    private byte[]? normalized;
    private readonly int portableBitLength577;
    public int BitLength => ownedHeapBits?.ReadHeapBitLength() ?? ownedResourceBits577?.ReadResourceBits577().BitLength ?? portableBitLength577;
    public bool IsBinary => BitLength % 8 == 0;

    public BitString(ReadOnlySpan<byte> data, int? bitLength = null)
    {
        portableBitLength577 = bitLength ?? checked(data.Length * 8);
        if (BitLength < 0 || BitLength > data.Length * 8 || (BitLength + 7) / 8 != data.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var owned = data.ToArray();
        if (owned.Length > 0 && BitLength % 8 != 0)
            owned[^1] &= (byte)(0xff << (8 - BitLength % 8));
        backing = new BinaryBacking(owned, owned.Length > BitStringStorage.HeapBytes);
    }

    public byte[] ToArray()
    {
        using IDisposable? read = backing.RetainNativeCopyRead524();

        return ByteSpan.ToArray();
    }

    internal int ByteLength => (int)(((long)BitLength + BitStringStorage.ByteRoundingOffset) / BitStringLayout.BitsPerByte);

    internal ReadOnlySpan<byte> ByteSpan
    {
        get
        {
            if (ownedHeapBits is not null)
                return ownedHeapBits.CopyHeapBytes();
            if (ownedResourceBits577 is not null)
                return ReadOwnedByteSpan577();
            if (LogicalBitOffset577 % BitStringLayout.BitsPerByte == 0 && IsBinary)
                return bytes.Slice((int)(LogicalBitOffset577 / BitStringLayout.BitsPerByte), ByteLength);
            var result = Volatile.Read(ref normalized);
            if (result is null)
            {
                result = new byte[ByteLength];
                for (int index = 0; index < BitLength; index++)
                    result[index / BitStringLayout.BitsPerByte] |= (byte)(Bit(index) << (BitStringStorage.HighBit - index % BitStringLayout.BitsPerByte));
                ObservedPayloadStorage.Binary(result);
                result = Interlocked.CompareExchange(ref normalized, result, null) ?? result;
            }

            return result;
        }
    }

    internal int ReferencedByteSize
    {
        get
        {
            ValidateOwnedBits577();

            return backing.IsReference ? checked((int)backing.ReferencedByteSize524) : BitLength / BitStringLayout.BitsPerByte;
        }
    }

    public BitString SliceBits(int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > BitLength - count)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (ownedHeapBits is not null)
            return SliceOwnedHeapBits(offset, count);
        if (ownedResourceBits577 is not null)
            return SliceOwnedResourceBits577(offset, count);
        var result = new BitString(
            backing,
            LogicalBitOffset577 + offset,
            count,
            sliceOrigin577: resourceSliceOrigin577
        );
        if (count > BitStringStorage.HeapBytes * BitStringLayout.BitsPerByte)
            return result;

        return new BitString(result.ByteSpan, count);
    }

    private BitString(
        BinaryBacking storage,
        long offset,
        int count,
        object? token = null,
        BitString? sliceOrigin577 = null
    )
    {
        backing = storage;
        writer = token;
        resourceSliceOrigin577 = sliceOrigin577;
        bitOffset = offset;
        portableBitLength577 = count;
    }

    private BitString(byte[] ownedBytes, bool takeOwnership)
    {
        var owned = takeOwnership ? ownedBytes : ownedBytes.ToArray();
        backing = new BinaryBacking(owned, owned.Length > BitStringStorage.HeapBytes);
        portableBitLength577 = checked(ownedBytes.Length * BitStringLayout.BitsPerByte);
    }

    internal static BitString FromOwnedBytes(byte[] bytes) => new(bytes, true);

    internal static BitString InitializeWritable(int initialBytes)
    {
        var initialized = BinaryBacking.InitializeWritable(initialBytes);

        return new BitString(
            initialized.Backing,
            0,
            0,
            initialized.Writer
        );
    }


    internal static BitString FromOwnedBacking(byte[] parent, int byteOffset, int bitLength)
    {
        if (byteOffset < 0 || bitLength < 0 || (long)byteOffset * BitStringLayout.BitsPerByte + bitLength > (long)parent.Length * BitStringLayout.BitsPerByte)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var storage = new BinaryBacking(parent, true);

        return new BitString(storage, (long)byteOffset * BitStringLayout.BitsPerByte, bitLength);
    }


    internal static BitString FromValidatedAppend(BitString prefix, BitString complete)
    {
        if (complete.BitLength == prefix.BitLength)
            return prefix;
        var payload = complete.ToArray();
        var next = prefix.backing.Append(prefix.writer, payload);

        return new BitString(
            next.Backing,
            0,
            complete.BitLength,
            next.Writer
        );
    }

    internal void PinForMatch() => backing.Pin();

    internal void PinForSharing() => backing.Pin();


    internal BitString ByteView() => IsBinary ? this : FromOwnedBytes(ToArray());

    public void WriteTo(Stream destination)
    {
        using IDisposable? read = backing.RetainNativeCopyRead524();
        destination.Write(ByteSpan);
    }

    internal int Bit(int i)
    {
        if (ownedHeapBits is not null)
            return (ownedHeapBits.ReadHeapByte(i / BitStringLayout.BitsPerByte) >> (BitStringStorage.HighBit - i % BitStringLayout.BitsPerByte)) & 1;
        long absolute497 = LogicalBitOffset577 + i;

        return (backing.ReadByte524((int)(absolute497 / BitStringLayout.BitsPerByte))
            >> (BitStringStorage.HighBit - (int)(absolute497 % BitStringLayout.BitsPerByte))) & 1;
    }

    public override string ToString()
    {
        var logical = ToArray();

        return IsBinary ? "<<" + string.Join(',', logical) + ">>" : "<<" + string.Join(
            ',',
            logical[..^1].Select(x => x.ToString(CultureInfo.InvariantCulture)).Append($"{logical[^1] >> (8 - BitLength % 8)}:{BitLength % 8}")
        ) + ">>";
    }
}
