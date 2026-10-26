namespace Erlang;

internal static class HamtViewLayout
{
    internal const int ReleaseFlatMapLimit = MapIteratorLayout.FlatMapMaximumEntries;
    internal const int WordBits = 64;
    internal const int NibbleBits = 4;
    internal const int NibbleMask = 15;
    internal const int HashLevels = WordBits / NibbleBits;
    internal const int SmallPayloadBits = 60;
    internal const int SmallValueBits = SmallPayloadBits - 1;
    internal const int SmallTag = 15;
    internal const int SmallTagBits = 4;
    internal const ulong UnsignedSmallMask = ulong.MaxValue >> SmallTagBits;
    internal const int DigitBytes = 8;
    internal const int PairShift = 32;
    internal const ulong NegativeBigType = 15;
    internal const ulong PositiveBigType = 16;
    internal const ulong AlphaMultiplier = 0x87C37B91114253D5;
    internal const ulong BetaMultiplier = 0x4CF5AD432745937F;
    internal const ulong FinalFirstMultiplier = 0xFF51AFD7ED558CCD;
    internal const ulong FinalSecondMultiplier = 0xC4CEB9FE1A85EC53;
    internal const ulong AlphaIncrement = 0x52DCE729;
    internal const ulong BetaIncrement = 0x38495AB5;
    internal const int AlphaInputRotation = 31;
    internal const int AlphaStateRotation = 27;
    internal const int BetaInputRotation = 33;
    internal const int BetaStateRotation = 31;
    internal const int StateMultiplier = 5;
    internal const int FinalShift = 33;
}
