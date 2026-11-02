namespace Erlang;

public sealed partial class BitString
{
    // Pure value copying only: this API grants no guest/native borrow authority.
    internal BitString CopyIdentityValue255()
    {
        if (ownedHeapBits is not null)
            return ownedHeapBits.CopyHeapValue255();
        if (ownedResourceBits577 is not null)
            return ownedResourceBits577.CopyResourceValue255();
        if (backing.NativeStorage524 is null && !backing.HasWritableOrigin
            && writer is null && resourceSliceOrigin577 is null)
            return this;

        // Detached views have immutable range fields. Their origin is not read
        // as a process-cell capability; consume the retained backing by value.
        using IDisposable? read = backing.RetainNativeCopyRead524();

        return backing.CopyValueBits255(bitOffset, portableBitLength577);
    }

    internal static BitString CopyValueRange255(ReadOnlySpan<byte> source, long offset, int bits)
    {
        if (offset < 0 || bits < 0 || offset > (long)source.Length * BitStringLayout.BitsPerByte - bits)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var copy = new byte[checked((int)(((long)bits + BitStringLayout.BitsPerByte - 1) / BitStringLayout.BitsPerByte))];
        if (offset % BitStringLayout.BitsPerByte == 0 && bits % BitStringLayout.BitsPerByte == 0)
            source.Slice(checked((int)(offset / BitStringLayout.BitsPerByte)), copy.Length).CopyTo(copy);
        else
        {
            for (int index = 0; index < bits; index++)
            {
                long position = checked(offset + index);
                int bit = (source[checked((int)(position / BitStringLayout.BitsPerByte))]
                    >> (BitStringStorage.HighBit - (int)(position % BitStringLayout.BitsPerByte))) & 1;
                copy[index / BitStringLayout.BitsPerByte] |= (byte)(bit << (BitStringStorage.HighBit - index % BitStringLayout.BitsPerByte));
            }
        }

        return new BitString(copy, bits);
    }
}
