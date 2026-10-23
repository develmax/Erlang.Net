using System.Buffers.Binary;

namespace Erlang;

// Pinned Win64 little-endian ErtsORefThing: 3 words, header=((3-1)<<6)|(4<<2),
// ordinary marker=~Uint32(0), followed by the three actual VM reference numbers.
// The public monitor reserves four pointers, but copy/compare use only 24 bytes.
internal static class ErlDriverMonitorLayout420
{
    internal const int PublicBytes = 32;
    internal const int PrefixBytes = 24;
    private const ulong OrdinaryHeader = (2UL << 6) | (4UL << 2);
    private const uint OrdinaryMarker = uint.MaxValue;

    internal static byte[] Encode(ReferenceTerm reference)
    {
        if (reference.Words.Count != ReferenceIdentityLayout.LocalWords)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var bytes = new byte[PrefixBytes];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, OrdinaryHeader);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), OrdinaryMarker);
        for (int index = 0; index < ReferenceIdentityLayout.LocalWords; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12 + index * sizeof(uint)), reference.Words[index]);

        return bytes;
    }
}
