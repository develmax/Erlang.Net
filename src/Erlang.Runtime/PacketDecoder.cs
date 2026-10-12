using System.Buffers.Binary;

namespace Erlang;

internal static class PacketDecoder
{
    public static Term Decode(Term selector, Term input, Term options)
    {
        PacketDecodeType type = Select(selector);
        if (input is not BitString binary || options is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var parsed = PacketDecodeOptions.Parse(options, type);
        if (!binary.IsBinary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ReadOnlySpan<byte> bytes = binary.ByteSpan;
        int size = Size(type, bytes, parsed);
        if (size < 0)
            return Term.Tuple(PacketDecodeAtoms.Error, PacketDecodeAtoms.Invalid);
        if (size == 0 || size > bytes.Length)
            return Term.Tuple(PacketDecodeAtoms.More, size == 0 ? PacketDecodeAtoms.Undefined : Term.I(size));
        int offset = type switch
        {
            PacketDecodeType.LengthOne => PacketDecodeParameters.OneByteHeader,
            PacketDecodeType.LengthTwo => PacketDecodeParameters.TwoByteHeader,
            PacketDecodeType.LengthFour => PacketDecodeParameters.FourByteHeader,
            _ => 0
        };
        int bodySize = size - offset;
        if (type == PacketDecodeType.FastCgi)
            bodySize -= bytes[PacketDecodeParameters.FastCgiPaddingOffset];
        if (bodySize < 0)
            return Term.Tuple(PacketDecodeAtoms.Error, PacketDecodeAtoms.Invalid);
        Term body = PacketHttpDecoder.IsHttp(type) ? PacketHttpDecoder.Parse(type, bytes[..size]) : type == PacketDecodeType.SslTls ? TlsBody(bytes, size) : offset == 0 && bodySize == bytes.Length ? binary : new BitString(bytes.Slice(offset, bodySize));

        return Term.Tuple(PacketDecodeAtoms.Ok, body, new BitString(bytes[size..]));
    }

    private static PacketDecodeType Select(Term selector)
    {
        if (selector is Integer value)
            return value.Value == PacketDecodeParameters.RawSelector ? PacketDecodeType.Raw :
                value.Value == PacketDecodeParameters.OneByteSelector ? PacketDecodeType.LengthOne :
                value.Value == PacketDecodeParameters.TwoByteSelector ? PacketDecodeType.LengthTwo :
                value.Value == PacketDecodeParameters.FourByteSelector ? PacketDecodeType.LengthFour :
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (selector is not Atom atom)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return atom.Name switch
        {
            PacketDecodeNames.Raw => PacketDecodeType.Raw,
            PacketDecodeNames.SunRecord => PacketDecodeType.SunRecord,
            PacketDecodeNames.Line => PacketDecodeType.Line,
            PacketDecodeNames.Asn1 => PacketDecodeType.Asn1,
            PacketDecodeNames.Cdr => PacketDecodeType.Cdr,
            PacketDecodeNames.FastCgi => PacketDecodeType.FastCgi,
            PacketDecodeNames.Tpkt => PacketDecodeType.Tpkt,
            PacketDecodeNames.SslTls => PacketDecodeType.SslTls,
            PacketDecodeNames.Http => PacketDecodeType.Http,
            PacketDecodeNames.HttpHeaders => PacketDecodeType.HttpHeaders,
            PacketDecodeNames.HttpBinary => PacketDecodeType.HttpBinary,
            PacketDecodeNames.HttpHeadersBinary => PacketDecodeType.HttpHeadersBinary,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };
    }

    private static int Size(PacketDecodeType type, ReadOnlySpan<byte> bytes, PacketDecodeOptions options)
    {
        switch (type)
        {
            case PacketDecodeType.Raw:
                return bytes.Length;
            case PacketDecodeType.LengthOne:
                return bytes.Length < PacketDecodeParameters.OneByteHeader ? 0 : Finish(PacketDecodeParameters.OneByteHeader, bytes[0], options.PacketSize);
            case PacketDecodeType.LengthTwo:
                return bytes.Length < PacketDecodeParameters.TwoByteHeader ? 0 : Finish(PacketDecodeParameters.TwoByteHeader, BinaryPrimitives.ReadUInt16BigEndian(bytes), options.PacketSize);
            case PacketDecodeType.LengthFour:
                return bytes.Length < PacketDecodeParameters.FourByteHeader ? 0 : Finish(PacketDecodeParameters.FourByteHeader, BinaryPrimitives.ReadUInt32BigEndian(bytes), options.PacketSize);
            case PacketDecodeType.SunRecord:
                return bytes.Length < PacketDecodeParameters.SunRecordHeader ? 0 : Finish(
                    PacketDecodeParameters.SunRecordHeader,
                    BinaryPrimitives.ReadUInt32BigEndian(bytes) & PacketDecodeParameters.SunRecordLengthMask,
                    options.PacketSize
                );
            case PacketDecodeType.Line:
                int end = bytes.IndexOf(options.Delimiter);
                int length = end < 0 ? bytes.Length : end + 1;
                if (options.PacketSize != 0 && length > options.PacketSize)
                    return PacketDecodeParameters.InvalidSize;
                if (options.LineLength != 0 && length >= options.LineLength)
                    return (int)options.LineLength;

                return end < 0 ? PacketDecodeParameters.UnknownSize : length;
            case PacketDecodeType.Asn1:
                return Asn1Size(bytes, options.PacketSize);
            case PacketDecodeType.Cdr:
                if (bytes.Length < PacketDecodeParameters.CdrHeader)
                    return PacketDecodeParameters.UnknownSize;
                if (!bytes.StartsWith(PacketDecodeParameters.CdrMagic))
                    return PacketDecodeParameters.InvalidSize;
                uint size = (bytes[PacketDecodeParameters.CdrFlagsOffset] & PacketDecodeParameters.CdrLittleEndianFlag) != 0 ? BinaryPrimitives.ReadUInt32LittleEndian(bytes[PacketDecodeParameters.CdrSizeOffset..]) : BinaryPrimitives.ReadUInt32BigEndian(bytes[PacketDecodeParameters.CdrSizeOffset..]);

                return Finish(PacketDecodeParameters.CdrHeader, size, options.PacketSize);
            case PacketDecodeType.FastCgi:
                if (bytes.Length < PacketDecodeParameters.FastCgiHeader)
                    return PacketDecodeParameters.UnknownSize;
                if (bytes[0] != PacketDecodeParameters.FastCgiVersion)
                    return PacketDecodeParameters.InvalidSize;

                return Finish(
                    PacketDecodeParameters.FastCgiHeader,
                    (uint)(BinaryPrimitives.ReadUInt16BigEndian(bytes[PacketDecodeParameters.FastCgiLengthOffset..]) + bytes[PacketDecodeParameters.FastCgiPaddingOffset]),
                    options.PacketSize
                );
            case PacketDecodeType.Tpkt:
                if (bytes.Length < PacketDecodeParameters.TpktHeader)
                    return PacketDecodeParameters.UnknownSize;
                if (bytes[0] != PacketDecodeParameters.TpktVersion)
                    return PacketDecodeParameters.InvalidSize;

                return Finish(
                    PacketDecodeParameters.TpktHeader,
                    unchecked((uint)(BinaryPrimitives.ReadUInt16BigEndian(bytes[PacketDecodeParameters.TpktLengthOffset..]) - PacketDecodeParameters.TpktHeader)),
                    options.PacketSize
                );
            case PacketDecodeType.SslTls:
                if (bytes.Length < PacketDecodeParameters.TlsHeader)
                    return PacketDecodeParameters.UnknownSize;
                uint payload = IsV2Hello(bytes) ? unchecked((uint)((BinaryPrimitives.ReadUInt16BigEndian(bytes) & PacketDecodeParameters.TlsV2LengthMask) - PacketDecodeParameters.TlsV2PayloadPrefixLength)) : BinaryPrimitives.ReadUInt16BigEndian(bytes[PacketDecodeParameters.TlsLengthOffset..]);

                return Finish(PacketDecodeParameters.TlsHeader, payload, options.PacketSize);
            case PacketDecodeType.Http:
            case PacketDecodeType.HttpBinary:
            case PacketDecodeType.HttpHeaders:
            case PacketDecodeType.HttpHeadersBinary:
                return PacketHttpDecoder.Size(type, bytes, options);
            default:
                throw new InvalidOperationException(PacketDecodeDiagnostics.UnexpectedSelector);
        }
    }

    private static int Finish(int header, uint payload, uint maximum)
    {
        if (maximum == 0)
            maximum = (uint)(int.MaxValue - header);

        return payload > maximum ? PacketDecodeParameters.InvalidSize : unchecked((int)((uint)header + payload));
    }

    private static int Asn1Size(ReadOnlySpan<byte> bytes, uint maximum)
    {
        if (bytes.Length < PacketDecodeParameters.Asn1MinimumHeader)
            return PacketDecodeParameters.UnknownSize;
        int position = 1;
        if ((bytes[0] & PacketDecodeParameters.Asn1LongTagMask) == PacketDecodeParameters.Asn1LongTagMask)
        {
            while (position < bytes.Length && (bytes[position] & PacketDecodeParameters.Asn1ContinuationMask) != 0)
                position++;
            if (bytes.Length - position < PacketDecodeParameters.Asn1MinimumHeader)
                return PacketDecodeParameters.UnknownSize;
            position++;
        }
        byte field = bytes[position++];
        uint length = (uint)(field & PacketDecodeParameters.Asn1LengthMask);
        if ((field & PacketDecodeParameters.Asn1ContinuationMask) != 0)
        {
            int count = (int)length;
            if (bytes.Length - position < count)
                return PacketDecodeParameters.UnknownSize;
            if (count > PacketDecodeParameters.Asn1MaximumLengthBytes)
                return PacketDecodeParameters.InvalidSize;
            length = 0;
            for (int index = 0; index < count; index++)
                length = (length << PacketDecodeParameters.ByteShift) | bytes[position++];
        }

        return Finish(position, length, maximum);
    }

    private static bool IsV2Hello(ReadOnlySpan<byte> bytes) => (bytes[0] & PacketDecodeParameters.TlsV2HeaderMask) != 0 && bytes[PacketDecodeParameters.TlsV2ClientHelloOffset] == PacketDecodeParameters.TlsV2ClientHello;

    private static Term TlsBody(ReadOnlySpan<byte> bytes, int size)
    {
        int type = bytes[0], major = bytes[PacketDecodeParameters.TlsMajorOffset], minor = bytes[PacketDecodeParameters.TlsMinorOffset];
        BitString payload;
        if (IsV2Hello(bytes))
        {
            int length = size - PacketDecodeParameters.TlsV2PayloadPrefixLength;
            if (length < 0)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            byte[] rewritten = new byte[PacketDecodeParameters.TlsHandshakePrefixLength + length];
            rewritten[0] = PacketDecodeParameters.TlsV2ClientHello;
            for (int index = 0; index < PacketDecodeParameters.TlsHandshakeLengthBytes; index++)
                rewritten[1 + index] = (byte)(length >> ((PacketDecodeParameters.TlsHandshakeLengthBytes - 1 - index) * PacketDecodeParameters.ByteShift));
            bytes.Slice(PacketDecodeParameters.TlsV2PayloadPrefixLength, length).CopyTo(rewritten.AsSpan(PacketDecodeParameters.TlsHandshakePrefixLength));
            payload = BitString.FromOwnedBytes(rewritten);
            type = PacketDecodeParameters.TlsHandshakeType;
            major = bytes[PacketDecodeParameters.TlsV2MajorOffset];
            minor = bytes[PacketDecodeParameters.TlsV2MinorOffset];
        }
        else
            payload = new BitString(bytes.Slice(PacketDecodeParameters.TlsHeader, size - PacketDecodeParameters.TlsHeader));

        return Term.Tuple(
            PacketDecodeAtoms.SslTls,
            Term.List(),
            Term.I(type),
            Term.Tuple(Term.I(major), Term.I(minor)),
            payload
        );
    }
}
