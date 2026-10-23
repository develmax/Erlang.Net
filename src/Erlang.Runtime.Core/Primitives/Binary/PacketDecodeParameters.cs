namespace Erlang;

internal static class PacketDecodeParameters
{
    public const int RawSelector = 0;
    public const int OneByteSelector = 1;
    public const int TwoByteSelector = 2;
    public const int FourByteSelector = 4;
    public const int OneByteHeader = 1;
    public const int TwoByteHeader = 2;
    public const int FourByteHeader = 4;
    public const int SunRecordHeader = 4;
    public const uint SunRecordLengthMask = 0x7fffffff;
    public const int Asn1MinimumHeader = 2;
    public const byte Asn1LongTagMask = 0x1f;
    public const byte Asn1ContinuationMask = 0x80;
    public const byte Asn1LengthMask = 0x7f;
    public const int Asn1MaximumLengthBytes = 4;
    public const int ByteShift = 8;
    public const int CdrHeader = 12;
    public const int CdrFlagsOffset = 6;
    public const byte CdrLittleEndianFlag = 1;
    public const int CdrSizeOffset = 8;
    public static ReadOnlySpan<byte> CdrMagic => "GIOP"u8;
    public const int FastCgiHeader = 8;
    public const byte FastCgiVersion = 1;
    public const int FastCgiLengthOffset = 4;
    public const int FastCgiPaddingOffset = 6;
    public const int TpktHeader = 4;
    public const byte TpktVersion = 3;
    public const int TpktLengthOffset = 2;
    public const int TlsHeader = 5;
    public const int TlsLengthOffset = 3;
    public const int TlsMajorOffset = 1;
    public const int TlsMinorOffset = 2;
    public const byte TlsV2HeaderMask = 0x80;
    public const int TlsV2ClientHelloOffset = 2;
    public const byte TlsV2ClientHello = 1;
    public const ushort TlsV2LengthMask = 0x7fff;
    public const int TlsV2PayloadPrefixLength = 3;
    public const int TlsV2MajorOffset = 3;
    public const int TlsV2MinorOffset = 4;
    public const byte TlsHandshakeType = 22;
    public const int TlsHandshakePrefixLength = 4;
    public const int TlsHandshakeLengthBytes = 3;
    public const byte DefaultLineDelimiter = (byte)'\n';
    public const int InvalidSize = -1;
    public const int UnknownSize = 0;
}
