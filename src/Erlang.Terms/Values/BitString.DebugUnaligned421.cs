namespace Erlang;

public sealed partial class BitString
{
    internal BitString DebugUnalignedCopy421(int offset, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        PinForSharing();
        long allocatedBits = Math.Max((long)BitLength + offset,
            (long)BitStringStorage.HeapBytes * BitStringLayout.BitsPerByte + 1);
        int allocatedBytes = checked((int)((allocatedBits + BitStringStorage.ByteRoundingOffset) / BitStringLayout.BitsPerByte));
        byte[] target = new byte[allocatedBytes];
        for (int index = 0; index < BitLength; index++)
        {
            if ((index & DebugUnalignedLayout421.CancellationMask) == 0)
                cancellation.ThrowIfCancellationRequested();
            if (Bit(index) != 0)
            {
                long destination = (long)index + offset;
                target[(int)(destination / BitStringLayout.BitsPerByte)] |=
                    (byte)(1 << (BitStringStorage.HighBit - (int)(destination % BitStringLayout.BitsPerByte)));
            }
        }
        cancellation.ThrowIfCancellationRequested();

        return new BitString(new BinaryBacking(target, true), offset, BitLength);
    }
}
