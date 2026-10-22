using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class BitString : Term
{
    private readonly BinaryBacking backing;
    private readonly object? writer;
    private byte[] bytes => backing.Buffer;
    private readonly int bitOffset;
    private byte[]? normalized;
    public int BitLength
    {
        get;
    }
    public bool IsBinary => BitLength % 8 == 0;

    public BitString(ReadOnlySpan<byte> data, int? bitLength = null)
    {
        BitLength = bitLength ?? checked(data.Length * 8);
        if (BitLength < 0 || BitLength > data.Length * 8 || (BitLength + 7) / 8 != data.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var owned = data.ToArray();
        backing = new BinaryBacking(owned, owned.Length > BitStringStorage.HeapBytes);
        if (bytes.Length > 0 && BitLength % 8 != 0)
            bytes[^1] &= (byte)(0xff << (8 - BitLength % 8));
    }

    public byte[] ToArray() => ByteSpan.ToArray();

    internal int ByteLength => (int)(((long)BitLength + BitStringStorage.ByteRoundingOffset) / BitStringLayout.BitsPerByte);

    internal ReadOnlySpan<byte> ByteSpan
    {
        get
        {
            if (bitOffset % BitStringLayout.BitsPerByte == 0 && IsBinary)
                return bytes.AsSpan(bitOffset / BitStringLayout.BitsPerByte, ByteLength);
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

    internal int ReferencedByteSize => backing.IsReference ? bytes.Length : BitLength / BitStringLayout.BitsPerByte;

    public BitString SliceBits(int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > BitLength - count)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var result = new BitString(backing, bitOffset + offset, count);
        if (count > BitStringStorage.HeapBytes * BitStringLayout.BitsPerByte)
            return result;

        return new BitString(result.ByteSpan, count);
    }

    private BitString(BinaryBacking storage, int offset, int count, object? token = null)
    {
        backing = storage;
        writer = token;
        bitOffset = offset;
        BitLength = count;
    }

    private BitString(byte[] ownedBytes, bool takeOwnership)
    {
        var owned = takeOwnership ? ownedBytes : ownedBytes.ToArray();
        backing = new BinaryBacking(owned, owned.Length > BitStringStorage.HeapBytes);
        BitLength = checked(ownedBytes.Length * BitStringLayout.BitsPerByte);
    }

    internal static BitString FromOwnedBytes(byte[] bytes) => new(bytes, true);

    internal static BitString FromValidatedAppend(BitString prefix, BitString complete)
    {
        if (complete.BitLength == prefix.BitLength)
            return prefix;
        var payload = complete.ToArray();
        var next = prefix.backing.Append(prefix.writer, payload);

        return new BitString(next.Backing, 0, complete.BitLength, next.Writer);
    }

    internal void PinForMatch() => backing.Pin();


    internal BitString ByteView() => IsBinary ? this : FromOwnedBytes(ToArray());

    public void WriteTo(Stream destination) => destination.Write(ByteSpan);

    internal int Bit(int i) => (bytes[(bitOffset + i) / BitStringLayout.BitsPerByte] >> (BitStringStorage.HighBit - (bitOffset + i) % BitStringLayout.BitsPerByte)) & 1;

    public override string ToString()
    {
        var logical = ToArray();

        return IsBinary ? "<<" + string.Join(',', logical) + ">>" : "<<" + string.Join(
            ',',
            logical[..^1].Select(x => x.ToString(CultureInfo.InvariantCulture)).Append($"{logical[^1] >> (8 - BitLength % 8)}:{BitLength % 8}")
        ) + ">>";
    }
}
