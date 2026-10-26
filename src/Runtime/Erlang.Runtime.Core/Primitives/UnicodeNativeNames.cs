namespace Erlang;

internal static class UnicodeNativeNames
{
    internal const string Module = "unicode";
    internal const string IsSevenBit = "bin_is_7bit";
    internal const string ToBinary = "characters_to_binary";
    internal const string ToList = "characters_to_list";
    internal const string ToBinaryTrap = "characters_to_binary_int";
    internal const string ToListTrap = "characters_to_list_int";
    internal const string Latin1 = "latin1";
    internal const string Unicode = "unicode";
    internal const string Utf8 = "utf8";
    internal const string Incomplete = "incomplete";
    internal const int WorkSlice = 1024;
    internal const int MaximumUtf8Bytes = 4;
    internal const int BitsPerByte = 8;
    internal const int AsciiLimit = 0x80;
    internal const byte TwoByteLead = 0xc0;
    internal const byte TwoByteLeadLimit = 0xe0;
    internal const byte ThreeByteLead = 0xe0;
    internal const byte ThreeByteLeadLimit = 0xf0;
    internal const byte FourByteLead = 0xf0;
    internal const byte FourByteLeadLimit = 0xf8;
    internal const byte ContinuationMask = 0xc0;
    internal const byte ContinuationTag = 0x80;
    internal const byte ContinuationPayloadMask = 0x3f;
    internal const byte LeadPayloadMask = 0x7f;
    internal const int ContinuationPayloadBits = 6;
    internal const int MinimumTwoByteScalar = 0x80;
    internal const int MinimumThreeByteScalar = 0x800;
    internal const int MinimumFourByteScalar = 0x10000;
    internal const int MaximumScalar = 0x10ffff;
    internal const int FirstSurrogate = 0xd800;
    internal const int LastSurrogate = 0xdfff;
    internal const long MinimumSmall = -(1L << 59);
    internal const long MaximumSmall = (1L << 59) - 1;
}
