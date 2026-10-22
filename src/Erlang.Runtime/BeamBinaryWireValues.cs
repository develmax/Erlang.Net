using System.Numerics;

namespace Erlang;

internal static class BeamBinaryWireValues
{
    internal static int ListFlags(BeamStructure structure, BeamOperand operand, bool allowSigned)
    {
        if (operand.Kind == BeamOperandParameters.Nil)
            return 0;
        if (operand.Kind != BeamOperandParameters.LiteralValue || operand.Value < 0 || operand.Value >= structure.Literals.Count)
            throw new NotSupportedException(BeamBinaryNames.Unsupported);
        int flags = 0;
        Term cursor = structure.Literals[(int)operand.Value];
        while (cursor is Cons cell)
        {
            int bit = cell.Head switch
            {
                Atom { Name: BeamBinaryFlagNames.Little } => BeamBinaryFlagNames.LittleBit,
                Atom { Name: BeamBinaryFlagNames.Native } => BeamBinaryFlagNames.NativeBit,
                Atom { Name: BeamBinaryFlagNames.Signed } when allowSigned => BeamBinaryFlagNames.SignedBit,
                _ => throw new NotSupportedException(BeamBinaryNames.Unsupported)
            };
            if ((flags & bit) != 0)
                throw new NotSupportedException(BeamBinaryNames.Unsupported);
            flags |= bit;
            cursor = cell.Tail;
        }
        if (cursor is not Nil)
            throw new NotSupportedException(BeamBinaryNames.Unsupported);

        return NumericFlags(flags);
    }

    internal static int NumericFlags(int flags)
    {
        if (flags is not (0 or BeamBinaryFlagNames.LittleBit or BeamBinaryFlagNames.SignedBit
            or BeamBinaryFlagNames.LittleSigned or BeamBinaryFlagNames.NativeBit or BeamBinaryFlagNames.NativeSigned))
            throw new NotSupportedException(BeamBinaryNames.Unsupported);
        if ((flags & BeamBinaryFlagNames.NativeBit) != 0)
            flags = (flags & ~BeamBinaryFlagNames.NativeBit) | (BitConverter.IsLittleEndian ? BeamBinaryFlagNames.LittleBit : 0);

        return flags;
    }

    internal static bool Little(int flags) => (flags & BeamBinaryFlagNames.LittleBit) != 0;

    internal static bool Signed(int flags) => (flags & BeamBinaryFlagNames.SignedBit) != 0;

    internal static BigInteger Integer(BeamBinaryMatchContext match, int bits, int flags)
    {
        byte[] bytes = match.Source.SliceBits(match.Position, bits).ToArray();
        int padding = (BeamBinaryNames.BitsPerByte - bits % BeamBinaryNames.BitsPerByte) % BeamBinaryNames.BitsPerByte;
        BigInteger value;
        if (Little(flags))
        {
            if (padding != 0)
                bytes[^1] >>= padding;
            value = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        }
        else
            value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true) >> padding;
        if (bits != 0 && Signed(flags) && value.GetBitLength() == bits)
            value -= BigInteger.One << bits;

        return value;
    }

    internal static BitString Float(Term value, int bits, bool little)
    {
        if (bits is not (BeamFloatStringNames.HalfBits or BeamFloatStringNames.SingleBits or BeamFloatStringNames.DoubleBits))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        double number;
        if (value is FloatTerm floating)
            number = floating.Value;
        else if (value is Integer integer && integer.TryToDouble(out double converted))
            number = converted;
        else
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ulong payload = bits switch
        {
            BeamFloatStringNames.HalfBits => BeamBinaryHalfValues.Encode(number),
            BeamFloatStringNames.SingleBits => BitConverter.SingleToUInt32Bits((float)number),
            _ => BitConverter.DoubleToUInt64Bits(number)
        };
        byte[] bytes = new byte[bits / BeamBinaryNames.BitsPerByte];
        for (int index = 0; index < bytes.Length; index++)
        {
            int shift = little ? index : bytes.Length - 1 - index;
            bytes[index] = (byte)(payload >> (shift * BeamBinaryNames.BitsPerByte));
        }

        return new BitString(bytes);
    }

    internal static int IntegerSourceBit(int bit, int bits, bool little)
    {
        if (!little)
            return bits - bit - 1;
        int start = bit / BeamBinaryNames.BitsPerByte * BeamBinaryNames.BitsPerByte;
        int width = Math.Min(BeamBinaryNames.BitsPerByte, bits - start);

        return start + width - 1 - (bit - start);
    }
}
