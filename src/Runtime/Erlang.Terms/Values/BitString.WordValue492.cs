namespace Erlang;

public sealed partial class BitString
{
    // Reads only one UTF16/UTF32 word through existing authenticated byte APIs.
    // Caller restricts mutability; unaligned ranges keep its original Bit route.
    internal bool TryReadAlignedWord492(
        int position,
        int bytes,
        bool little,
        out uint value
    )
    {
        value = 0;
        if (bytes is not (sizeof(ushort) or sizeof(uint))) return false;
        ResourceBitsRead577 range = CaptureResourceBits577();
        int bits = bytes * BitStringLayout.BitsPerByte;
        if (position < 0 || position > range.BitLength - bits) return false;
        long start = range.BitOffset + position;
        if (start % BitStringLayout.BitsPerByte != 0) return false;
        int first = (int)(start / BitStringLayout.BitsPerByte);
        for (int index = 0; index < bytes; index++)
        {
            uint next = ownedHeapBits is not null
                ? ownedHeapBits.ReadHeapByte(first + index)
                : backing.ReadByte524(first + index);
            if (little)
                value |= next << (index * BitStringLayout.BitsPerByte);
            else
                value = (value << BitStringLayout.BitsPerByte) | next;
        }

        return true;
    }
}
