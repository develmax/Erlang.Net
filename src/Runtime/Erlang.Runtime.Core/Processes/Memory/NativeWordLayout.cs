namespace Erlang;

// OTP29.1.1 Eterm/ErtsDigit widths and little-endian payload packing.
internal static class NativeWordLayout
{
    internal const int ByteBits = 8;
    internal const int UintBits = 32;
    internal static int Bytes => IntPtr.Size;
    internal static int Bits => Bytes * ByteBits;
    internal static int UintsPerWord => Bytes / sizeof(uint);
    internal static int DoubleWords => sizeof(double) / Bytes;
    internal static bool Supported => BitConverter.IsLittleEndian && Bytes is sizeof(uint) or sizeof(ulong);

    internal static nuint[] Encode64(ulong value) => Bytes == sizeof(ulong)
        ? [(nuint)value] : [(nuint)(uint)value, (nuint)(uint)(value >> UintBits)];

    internal static ulong Decode64(nuint low, nuint high = 0) => Bytes == sizeof(ulong)
        ? (ulong)low : (uint)low | ((ulong)(uint)high << UintBits);

    internal static nuint[] PackUints(IReadOnlyList<uint> values)
    {
        var result = new nuint[(values.Count + UintsPerWord - 1) / UintsPerWord];
        for (int index = 0; index < values.Count; index++)
            result[index / UintsPerWord] |= (nuint)values[index] << (index % UintsPerWord * UintBits);

        return result;
    }

    internal static uint ReadUint(nuint[] words, int index)
        => (uint)(words[index / UintsPerWord] >> (index % UintsPerWord * UintBits));
}
