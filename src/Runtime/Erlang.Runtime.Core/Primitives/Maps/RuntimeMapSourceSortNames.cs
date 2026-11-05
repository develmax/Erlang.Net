namespace Erlang;

internal static class RuntimeMapSourceSortNames
{
    internal const int SelectionCutoff = 16;
    internal const int ImprovedSeedThreshold = 128;
    internal const uint InitialSeed = 1;
    internal const int ComplementShift = 18;
    internal const int FirstXorShift = 31;
    internal const int FoldLowShift = 2;
    internal const int FoldHighShift = 4;
    internal const int SecondXorShift = 11;
    internal const int FinalFoldShift = 6;
    internal const int FinalXorShift = 22;
    internal const ulong HighNibbles = 0xF0F0F0F0F0F0F0F0UL;
    internal const int NibbleBits = 4;
}
