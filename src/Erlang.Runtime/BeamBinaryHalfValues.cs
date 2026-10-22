namespace Erlang;

internal static class BeamBinaryHalfValues
{
    internal static ushort Encode(double value)
    {
        ulong source = BitConverter.DoubleToUInt64Bits(value);
        int sign = (int)(source >> BeamHalfWire.SourceSignShift) & BeamHalfWire.Sign;
        int exponent = (int)(source >> BeamHalfWire.SourceFractionBits) & BeamHalfWire.SourceExponentMask;
        if (exponent == 0)
            return (ushort)sign;
        ulong significant = (source & BeamHalfWire.SourceFractionMask) | (1UL << BeamHalfWire.SourceFractionBits);
        int power = exponent - BeamHalfWire.SourceBias;
        if (power >= BeamHalfWire.MinimumExponent)
        {
            if (power > BeamHalfWire.MaximumExponent)
                return (ushort)(sign | BeamHalfWire.Infinity);
            ulong kept = Round(significant, BeamHalfWire.NormalShift);
            if (kept == BeamHalfWire.CarrySignificant)
            {
                kept = BeamHalfWire.MinimumSignificant;
                if (++power > BeamHalfWire.MaximumExponent)
                    return (ushort)(sign | BeamHalfWire.Infinity);
            }

            return (ushort)(sign | ((power + BeamHalfWire.Bias) << BeamHalfWire.FractionBits) | ((int)kept & BeamHalfWire.FractionMask));
        }
        int shift = BeamHalfWire.SubnormalShiftBase - power;
        if (shift > BeamHalfWire.MaximumShift)
            return (ushort)sign;

        return (ushort)(sign | (int)Round(significant, shift));
    }

    private static ulong Round(ulong significant, int shift)
    {
        ulong kept = significant >> shift;
        ulong lower = significant & ((1UL << shift) - 1);
        ulong half = 1UL << (shift - 1);
        if (lower > half || lower == half && (kept & 1) != 0)
            kept++;

        return kept;
    }
}
