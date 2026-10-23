namespace Erlang;

internal static class BeamHalfWire
{
    internal const int SourceSignShift = 48;
    internal const int SourceFractionBits = 52;
    internal const int SourceExponentMask = 0x7ff;
    internal const int SourceBias = 1023;
    internal const ulong SourceFractionMask = (1UL << SourceFractionBits) - 1;
    internal const int Sign = 0x8000;
    internal const int Infinity = 0x7c00;
    internal const int MinimumExponent = -14;
    internal const int MaximumExponent = 15;
    internal const int Bias = 15;
    internal const int FractionBits = 10;
    internal const int FractionMask = (1 << FractionBits) - 1;
    internal const int MinimumSignificant = 1 << FractionBits;
    internal const int CarrySignificant = MinimumSignificant << 1;
    internal const int NormalShift = SourceFractionBits - FractionBits;
    internal const int SubnormalShiftBase = 28;
    internal const int MaximumShift = 63;
}
