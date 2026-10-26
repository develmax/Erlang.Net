namespace Erlang;

internal static class BeamUnicodeFieldNames
{
    internal const string Little = "little";
    internal const int LittleFlag = 2;
    internal const int WordBits = 16;
    internal const int ScalarBits = 32;
    internal const int HighSurrogateMaximum = 0xdbff;
    internal const int LowSurrogateMinimum = 0xdc00;
    internal const int SupplementaryMinimum = 0x10000;
    internal const int SurrogatePayloadBits = 10;
    internal const int SurrogatePayloadMask = 0x3ff;
}
