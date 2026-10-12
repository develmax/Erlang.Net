using System.Text;

namespace Erlang;

internal static class PacketHttpDecoder
{
    public static bool IsHttp(PacketDecodeType type) => type is PacketDecodeType.Http or PacketDecodeType.HttpBinary or PacketDecodeType.HttpHeaders or PacketDecodeType.HttpHeadersBinary;

    private static bool IsHeader(PacketDecodeType type) => type is PacketDecodeType.HttpHeaders or PacketDecodeType.HttpHeadersBinary;

    private static bool IsSpace(byte value) => value is PacketHttpSyntax.Space or PacketHttpSyntax.Tab;

    private static bool IsToken(byte value) => value > PacketHttpSyntax.Space && value < PacketHttpSyntax.AsciiUpperBound && !PacketHttpSyntax.SpecialCharacters.Contains(value);

    public static int Size(PacketDecodeType type, ReadOnlySpan<byte> bytes, PacketDecodeOptions options)
    {
        if (bytes.Length == 1 && bytes[0] == PacketHttpSyntax.Newline || bytes.Length == 2 && bytes[0] == PacketHttpSyntax.CarriageReturn && bytes[1] == PacketHttpSyntax.Newline)
            return bytes.Length;
        uint maximum = options.PacketSize == 0 ? options.LineLength : options.PacketSize;
        int start = 0;
        while (true)
        {
            int found = bytes[start..].IndexOf(PacketHttpSyntax.Newline);
            if (found < 0)
                return maximum != 0 && bytes.Length >= maximum ? PacketDecodeParameters.InvalidSize : PacketDecodeParameters.UnknownSize;
            int length = start + found + 1;
            if (!IsHeader(type))
                return maximum != 0 && length > maximum ? PacketDecodeParameters.InvalidSize : length;
            if (length < bytes.Length)
            {
                if (length > 2 && IsSpace(bytes[length]))
                {
                    start = length;
                    continue;
                }

                return maximum != 0 && length > maximum ? PacketDecodeParameters.InvalidSize : length;
            }

            return maximum != 0 && length > maximum ? PacketDecodeParameters.InvalidSize : PacketDecodeParameters.UnknownSize;
        }
    }

    public static Term Parse(PacketDecodeType type, ReadOnlySpan<byte> packet)
    {
        bool binary = type is PacketDecodeType.HttpBinary or PacketDecodeType.HttpHeadersBinary;
        int length = packet.Length;
        if (length >= 2 && packet[length - 2] == PacketHttpSyntax.CarriageReturn)
            length -= 2;
        else if (length >= 1 && packet[length - 1] == PacketHttpSyntax.Newline)
            length--;
        ReadOnlySpan<byte> line = packet[..length];
        Term? parsed = IsHeader(type) ? Header(line, binary) : Start(line, binary);

        return parsed ?? Term.Tuple(PacketHttpAtoms.Error, Text(packet, binary));
    }

    private static Term Text(ReadOnlySpan<byte> bytes, bool binary)
    {
        if (binary)
            return new BitString(bytes);
        Term result = Term.List();
        for (int index = bytes.Length - 1; index >= 0; index--)
            result = new Cons(Term.I(bytes[index]), result);

        return result;
    }

    private static void Spaces(ReadOnlySpan<byte> line, ref int position)
    {
        while (position < line.Length && IsSpace(line[position]))
            position++;
    }

    private static bool Number(ReadOnlySpan<byte> line, ref int position, out int value)
    {
        int start = position;
        value = 0;
        while (position < line.Length && line[position] is >= PacketHttpSyntax.DigitZero and <= PacketHttpSyntax.DigitNine)
            value = unchecked(value * PacketHttpSyntax.DecimalBase + line[position++] - PacketHttpSyntax.DigitZero);

        return position != start;
    }

    private static bool Version(
        ReadOnlySpan<byte> line,
        ref int position,
        out int major,
        out int minor
    )
    {
        major = minor = 0;
        if (!Number(line, ref position, out major) || position == line.Length || line[position++] != PacketHttpSyntax.Dot)
            return false;

        return Number(line, ref position, out minor);
    }

    private static Term? Start(ReadOnlySpan<byte> line, bool binary)
    {
        int position = 0;
        if (line.StartsWith(PacketHttpSyntax.VersionPrefix))
        {
            position = PacketHttpSyntax.VersionPrefix.Length;
            if (!Version(
                line,
                ref position,
                out int major,
                out int minor
            ))
                return null;
            int before = position;
            Spaces(line, ref position);
            if (before == position)
                return null;
            Number(line, ref position, out int status);
            before = position;
            Spaces(line, ref position);
            if (before == position && position < line.Length)
                return null;

            return Term.Tuple(
                PacketHttpAtoms.Response,
                Term.Tuple(Term.I(major), Term.I(minor)),
                Term.I(status),
                Text(line[position..], binary)
            );
        }
        while (position < line.Length && IsToken(line[position]))
            position++;
        if (position == 0 || position == line.Length || !IsSpace(line[position]))
            return null;
        ReadOnlySpan<byte> method = line[..position];
        Spaces(line, ref position);
        int uriStart = position;
        while (position < line.Length && !IsSpace(line[position]))
            position++;
        if (uriStart == position)
            return null;
        ReadOnlySpan<byte> uri = line[uriStart..position];
        Spaces(line, ref position);
        int requestMajor = PacketHttpSyntax.LegacyMajor, requestMinor = PacketHttpSyntax.LegacyMinor;
        if (position < line.Length)
        {
            if (line.Length - position < PacketHttpSyntax.MinimumVersionLength || !line[position..].StartsWith(PacketHttpSyntax.VersionPrefix))
                return null;
            position += PacketHttpSyntax.VersionPrefix.Length;
            if (!Version(
                line,
                ref position,
                out requestMajor,
                out requestMinor
            ))
                return null;
        }
        int known = Array.IndexOf(PacketHttpCatalog.Methods, Encoding.Latin1.GetString(method));
        Term methodTerm = known < 0 ? Text(method, binary) : PacketHttpCatalog.MethodAtoms[known];

        return Term.Tuple(
            PacketHttpAtoms.Request,
            methodTerm,
            Uri(uri, binary),
            Term.Tuple(Term.I(requestMajor), Term.I(requestMinor))
        );
    }

    private static Term? Header(ReadOnlySpan<byte> line, bool binary)
    {
        if (line.IsEmpty)
            return PacketHttpAtoms.EndOfHeaders;
        int length = 0;
        while (length < line.Length && IsToken(line[length]))
            length++;
        if (length == line.Length || line[length] != PacketHttpSyntax.Colon)
            return null;
        ReadOnlySpan<byte> original = line[..length];
        byte[] normalized = original.ToArray();
        if (length <= PacketHttpSyntax.MaximumNormalizedName)
        {
            bool upper = true;
            for (int index = 0; index < normalized.Length; index++)
            {
                byte value = normalized[index];
                if (upper)
                {
                    if (value is >= PacketHttpSyntax.LowerA and <= PacketHttpSyntax.LowerZ)
                        normalized[index] -= PacketHttpSyntax.AsciiCaseDelta;
                    upper = false;
                }
                else if (value is >= PacketHttpSyntax.UpperA and <= PacketHttpSyntax.UpperZ)
                    normalized[index] += PacketHttpSyntax.AsciiCaseDelta;
                else if (value == PacketHttpSyntax.Hyphen)
                    upper = true;
            }
        }
        int known = length <= PacketHttpSyntax.MaximumNormalizedName ? Array.IndexOf(PacketHttpCatalog.Headers, Encoding.Latin1.GetString(normalized)) : -1;
        int position = length + 1;
        Spaces(line, ref position);

        return Term.Tuple(
            PacketHttpAtoms.Header,
            Term.I(known + 1),
            known < 0 ? Text(normalized, binary) : PacketHttpCatalog.HeaderAtoms[known],
            Text(original, binary),
            Text(line[position..], binary)
        );
    }

    private static bool Prefix(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> prefix)
    {
        if (bytes.Length < prefix.Length)
            return false;
        for (int index = 0; index < prefix.Length; index++)
        {
            byte value = bytes[index];
            if (value is >= PacketHttpSyntax.UpperA and <= PacketHttpSyntax.UpperZ)
                value += PacketHttpSyntax.AsciiCaseDelta;
            if (value != prefix[index])
                return false;
        }

        return true;
    }

    private static Term Uri(ReadOnlySpan<byte> bytes, bool binary)
    {
        if (bytes.Length == 1 && bytes[0] == PacketHttpSyntax.Star)
            return PacketHttpAtoms.Star;
        if (bytes.Length <= 1 || bytes[0] == PacketHttpSyntax.Slash)
            return Term.Tuple(PacketHttpAtoms.AbsolutePath, Text(bytes, binary));
        bool http = Prefix(bytes, PacketHttpSyntax.HttpPrefix), https = Prefix(bytes, PacketHttpSyntax.HttpsPrefix);
        if (http || https)
            return AbsoluteUri(bytes[(http ? PacketHttpSyntax.HttpPrefix.Length : PacketHttpSyntax.HttpsPrefix.Length)..], binary, http);
        int colon = bytes.IndexOf(PacketHttpSyntax.Colon);

        return colon < 0 ? Text(bytes, binary) : Term.Tuple(PacketHttpAtoms.Scheme, Text(bytes[..colon], binary), Text(bytes[(colon + 1)..], binary));
    }

    private static Term AbsoluteUri(ReadOnlySpan<byte> bytes, bool binary, bool http)
    {
        int slash = bytes.IndexOf(PacketHttpSyntax.Slash);
        ReadOnlySpan<byte> path = slash < 0 ? PacketHttpSyntax.DefaultPath : bytes[slash..];
        ReadOnlySpan<byte> host = slash < 0 ? bytes : bytes[..slash];
        int colon = host.IndexOf(PacketHttpSyntax.Colon), port = 0;
        if (colon >= 0)
        {
            int hostLength = colon, portStart = colon + 1;
            if (!host.IsEmpty && host[0] == PacketHttpSyntax.OpenBracket && host.IndexOf(PacketHttpSyntax.CloseBracket) is int close && close >= 0)
            {
                hostLength = close + 1;
                portStart = host.LastIndexOf(PacketHttpSyntax.Colon) == close + 1 ? close + 2 : host.Length;
            }
            if (portStart < host.Length)
            {
                int position = portStart;
                Number(host, ref position, out int value);
                if (position == host.Length)
                    port = value;
            }
            host = host[..hostLength];
        }

        return Term.Tuple(
            PacketHttpAtoms.AbsoluteUri,
            http ? PacketHttpAtoms.HttpScheme : PacketHttpAtoms.HttpsScheme,
            Text(host, binary),
            port == 0 ? PacketHttpAtoms.UndefinedPort : Term.I(port),
            Text(path, binary)
        );
    }
}
