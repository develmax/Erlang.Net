namespace Erlang;

internal static class BeamBinaryFlagNames
{
    internal const string Little = "little";
    internal const string Native = "native";
    internal const string Signed = "signed";
    internal const int LittleBit = 2;
    internal const int SignedBit = 4;
    internal const int NativeBit = 16;
    internal const int LittleSigned = LittleBit | SignedBit;
    internal const int NativeSigned = NativeBit | SignedBit;
    internal const string InvalidFloatSize = "invalid";
}
