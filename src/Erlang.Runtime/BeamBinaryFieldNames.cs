namespace Erlang;

internal static class BeamBinaryFieldNames
{
    internal const int Utf8AsciiMaximum = 0x7f;
    internal const int Utf8TwoByteMinimum = 0xc2;
    internal const int Utf8TwoByteMaximum = 0xdf;
    internal const int Utf8ThreeByteMinimum = 0xe0;
    internal const int Utf8ThreeByteMaximum = 0xef;
    internal const int Utf8FourByteMinimum = 0xf0;
    internal const int Utf8FourByteMaximum = 0xf4;
    internal const int Utf8ContinuationMask = 0xc0;
    internal const int Utf8ContinuationTag = 0x80;
    internal const int Utf8ContinuationPayload = 0x3f;
    internal const int Utf8TwoByteLeadPayload = 0x1f;
    internal const int Utf8ThreeByteLeadPayload = 0x0f;
    internal const int Utf8FourByteLeadPayload = 0x07;
    internal const int Utf8ContinuationBits = 6;
    internal const int Utf8TwoByteScalarMinimum = 0x80;
    internal const int Utf8ThreeByteScalarMinimum = 0x800;
    internal const int Utf8FourByteScalarMinimum = 0x10000;
    internal const int SurrogateMinimum = 0xd800;
    internal const int SurrogateMaximum = 0xdfff;
    internal const int UnicodeScalarMaximum = 0x10ffff;
}
