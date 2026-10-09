using System.Buffers.Binary;
using System.Numerics;

namespace Erlang.Compiler;

internal static class BitFloat
{
    internal static double IntegerToDouble(BigInteger value)
    {
        BigInteger magnitude = BigInteger.Abs(value);
        long width = magnitude.GetBitLength();
        if (width <= 53) return (double)value;
        if (width > 1024) return value.Sign < 0 ? double.NegativeInfinity : double.PositiveInfinity;
        int shift = (int)width - 53;
        BigInteger significant = magnitude >> shift;
        BigInteger remainder = magnitude - (significant << shift), halfway = BigInteger.One << (shift - 1);
        if (remainder > halfway || remainder == halfway && !significant.IsEven) significant++;
        double result = Math.ScaleB((double)significant, shift);
        return value.Sign < 0 ? -result : result;
    }

    public static byte[] Encode(Term value, int width, string endian)
    {
        double number = value switch { FloatTerm f => f.Value, Integer i => IntegerToDouble(i.Value), _ => throw new ErlangException("badarg") };
        if (!double.IsFinite(number) || width is not (16 or 32 or 64)) throw new ErlangException("badarg");
        var bytes = new byte[width / 8];
        ulong bits = width switch
        {
            16 => BitConverter.HalfToUInt16Bits((Half)number),
            32 => BitConverter.SingleToUInt32Bits((float)number),
            _ => BitConverter.DoubleToUInt64Bits(number)
        };
        // Overflow after narrowing is retained in the binary, as in the pinned OTP helpers/tests.
        bool little = endian == "little" || endian == "native" && BitConverter.IsLittleEndian;
        for (int i = 0; i < bytes.Length; i++) bytes[little ? i : bytes.Length - 1 - i] = (byte)(bits >> (i * 8));
        return bytes;
    }

    public static FloatTerm? Decode(ReadOnlySpan<byte> bytes, int width, string endian)
    {
        if (width == 0) return new FloatTerm(0.0); // The reference matcher accepts a zero-width float.
        if (width is not (16 or 32 or 64)) return null;
        bool little = endian == "little" || endian == "native" && BitConverter.IsLittleEndian;
        double number = width switch
        {
            16 => (double)BitConverter.UInt16BitsToHalf(little ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes)),
            32 => BitConverter.UInt32BitsToSingle(little ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes)),
            _ => BitConverter.UInt64BitsToDouble(little ? BinaryPrimitives.ReadUInt64LittleEndian(bytes) : BinaryPrimitives.ReadUInt64BigEndian(bytes))
        };
        return double.IsFinite(number) ? new FloatTerm(number) : null;
    }
}
