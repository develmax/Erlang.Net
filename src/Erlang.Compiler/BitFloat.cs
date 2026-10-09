using System.Buffers.Binary;

namespace Erlang.Compiler;

internal static class BitFloat
{
    public static byte[] Encode(Term value, int width, string endian)
    {
        double number = value switch
        {
            FloatTerm f => f.Value,
            Integer i when i.TryToDouble(out double converted) => converted,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };

        if (!double.IsFinite(number) || width is not (FloatSegmentWidths.Half or FloatSegmentWidths.Single or FloatSegmentWidths.Double))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        var bytes = new byte[width / BitStorageLayout.BitsPerByte];
        ulong bits = width switch
        {
            FloatSegmentWidths.Half => BitConverter.HalfToUInt16Bits((Half)number),
            FloatSegmentWidths.Single => BitConverter.SingleToUInt32Bits((float)number),
            _ => BitConverter.DoubleToUInt64Bits(number)
        };

        // Overflow after narrowing is retained in the binary, as in the pinned OTP helpers/tests.

        bool little = endian == BitByteOrders.Little || endian == BitByteOrders.Native && BitConverter.IsLittleEndian;
        for (int i = 0; i < bytes.Length; i++)
            bytes[little ? i : bytes.Length - 1 - i] = (byte)(bits >> (i * BitStorageLayout.BitsPerByte));

        return bytes;
    }

    public static FloatTerm? Decode(ReadOnlySpan<byte> bytes, int width, string endian)
    {
        if (width == 0)
            return new FloatTerm(0.0); // The reference matcher accepts a zero-width float.

        if (width is not (FloatSegmentWidths.Half or FloatSegmentWidths.Single or FloatSegmentWidths.Double))
            return null;

        bool little = endian == BitByteOrders.Little || endian == BitByteOrders.Native && BitConverter.IsLittleEndian;
        double number = width switch
        {
            FloatSegmentWidths.Half => (double)BitConverter.UInt16BitsToHalf(little ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes)),
            FloatSegmentWidths.Single => BitConverter.UInt32BitsToSingle(little ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes)),
            _ => BitConverter.UInt64BitsToDouble(little ? BinaryPrimitives.ReadUInt64LittleEndian(bytes) : BinaryPrimitives.ReadUInt64BigEndian(bytes))
        };

        return double.IsFinite(number) ? new FloatTerm(number) : null;
    }
}
