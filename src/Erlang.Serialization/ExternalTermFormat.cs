using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;

namespace Erlang;

public static class ExternalTermFormat
{
    public const int DefaultMaxBytes = 16 * 1024 * 1024;

    public static byte[] Encode(Term term)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(131);
        Write(stream, term, 0);

        return stream.ToArray();
    }

    public static Term Decode(ReadOnlySpan<byte> data, int maxBytes = DefaultMaxBytes)
    {
        if (data.Length > maxBytes || data.Length < 2 || data[0] != 131)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        try
        {
            if (data[1] == 80)
            {
                if (data.Length < 6)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                uint expected = BinaryPrimitives.ReadUInt32BigEndian(data[2..]);
                if (expected > maxBytes)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                using var input = new MemoryStream(data[6..].ToArray());
                using var z = new ZLibStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                byte[] buffer = new byte[4096];
                int n;
                while ((n = z.Read(buffer)) != 0)
                {
                    if (output.Length + n > expected)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    output.Write(buffer, 0, n);
                }
                if (output.Length != expected)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var bytes = output.ToArray();
                var compressedReader = new Reader(bytes, maxBytes);
                var result = compressedReader.Read(0);
                if (!compressedReader.AtEnd)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return result;
            }
            var reader = new Reader(data[1..], maxBytes);
            var term = reader.Read(0);
            if (!reader.AtEnd)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return term;
        }
        catch (Exception ex) when (ex is EndOfStreamException or OverflowException or DecoderFallbackException or InvalidDataException or FormatException or ArgumentException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private static void U16(Stream s, int value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, checked((ushort)value));
        s.Write(b);
    }

    private static void U32(Stream s, uint value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(b, value);
        s.Write(b);
    }

    private static void Write(Stream s, Term term, int depth)
    {
        if (depth > 256)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        switch (term)
        {
            case Atom a:
                {
                    byte[] b = Encoding.UTF8.GetBytes(a.Name);
                    if (a.Name.EnumerateRunes().Count() > 255)
                        throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    s.WriteByte(118);
                    U16(s, b.Length);
                    s.Write(b);
                    break;
                }
            case Integer i when i.Value >= 0 && i.Value <= 255:
                s.WriteByte(97);
                s.WriteByte((byte)i.Value);
                break;
            case Integer i when i.Value >= int.MinValue && i.Value <= int.MaxValue:
                s.WriteByte(98);
                U32(s, unchecked((uint)(int)i.Value));
                break;
            case Integer i:
                {
                    byte[] b = BigInteger.Abs(i.Value).ToByteArray(true, false);
                    if (b.Length < 256)
                    {
                        s.WriteByte(110);
                        s.WriteByte((byte)b.Length);
                    }
                    else
                    {
                        s.WriteByte(111);
                        U32(s, (uint)b.Length);
                    }
                    s.WriteByte((byte)(i.Value.Sign < 0 ? 1 : 0));
                    s.Write(b);
                    break;
                }
            case FloatTerm f:
                {
                    s.WriteByte(70);
                    Span<byte> b = stackalloc byte[8];
                    BinaryPrimitives.WriteInt64BigEndian(b, BitConverter.DoubleToInt64Bits(f.Value));
                    s.Write(b);
                    break;
                }
            case Nil:
                s.WriteByte(106);
                break;
            case TupleTerm t:
                if (t.Items.Count < 256)
                {
                    s.WriteByte(104);
                    s.WriteByte((byte)t.Items.Count);
                }
                else
                {
                    s.WriteByte(105);
                    U32(s, (uint)t.Items.Count);
                }
                foreach (var item in t.Items)
                    Write(s, item, depth + 1);
                break;
            case Cons:
                {
                    var items = new List<Term>();
                    Term tail = term;
                    while (tail is Cons c)
                    {
                        items.Add(c.Head);
                        tail = c.Tail;
                    }
                    s.WriteByte(108);
                    U32(s, (uint)items.Count);
                    foreach (var item in items)
                        Write(s, item, depth + 1);
                    Write(s, tail, depth + 1);
                    break;
                }
            case MapTerm m:
                s.WriteByte(116);
                U32(s, (uint)m.Entries.Count);
                foreach (var e in m.Entries)
                {
                    Write(s, e.Key, depth + 1);
                    Write(s, e.Value, depth + 1);
                }
                break;
            case BitString b:
                s.WriteByte((byte)(b.IsBinary ? 109 : 77));
                byte[] data = b.ToArray();
                U32(s, (uint)data.Length);
                if (!b.IsBinary)
                    s.WriteByte((byte)(b.BitLength % 8));
                s.Write(data);
                break;
            case Pid p:
                s.WriteByte(88);
                Write(s, Term.A(p.Node), depth + 1);
                U32(s, (uint)p.Id);
                U32(s, (uint)(p.Id >> 32));
                U32(s, p.Creation);
                break;
            case PortTerm p:
                s.WriteByte(120);
                Write(s, Term.A(p.Node), depth + 1);
                Span<byte> id = stackalloc byte[8];
                BinaryPrimitives.WriteUInt64BigEndian(id, p.Id);
                s.Write(id);
                U32(s, p.Creation);
                break;
            case ReferenceTerm r:
                s.WriteByte(90);
                U16(s, r.Id > uint.MaxValue ? 2 : 1);
                Write(s, Term.A(r.Node), depth + 1);
                U32(s, r.Creation);
                U32(s, (uint)r.Id);
                if (r.Id > uint.MaxValue)
                    U32(s, (uint)(r.Id >> 32));
                break;
            default:
                throw new NotSupportedException(EtfDiagnostics.UnsupportedEncoding(term.GetType().Name));
        }
    }

    private ref struct Reader(ReadOnlySpan<byte> data, int limit)
    {
        private readonly ReadOnlySpan<byte> data = data; private int offset;
        public bool AtEnd => offset == data.Length;

        private byte Byte()
        {
            if (offset >= data.Length)
                throw new EndOfStreamException();

            return data[offset++];
        }

        private ReadOnlySpan<byte> Bytes(int n)
        {
            if (n < 0 || n > limit || n > data.Length - offset)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var b = data.Slice(offset, n);
            offset += n;

            return b;
        }

        private int U16() => BinaryPrimitives.ReadUInt16BigEndian(Bytes(2));

        private uint U32() => BinaryPrimitives.ReadUInt32BigEndian(Bytes(4));

        private int Count(uint n, int minimum = 1)
        {
            if (n > limit || n > int.MaxValue || n * (ulong)minimum > (ulong)(data.Length - offset))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return (int)n;
        }

        private string Node(int depth) => Read(depth) is Atom a ? a.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);

        public Term Read(int depth)
        {
            if (depth > 256)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            int tag = Byte();
            switch (tag)
            {
                case 97:
                    return Term.I(Byte());
                case 98:
                    return Term.I(unchecked((int)U32()));
                case 70:
                    return new FloatTerm(BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(Bytes(8))));
                case 99:
                    return new FloatTerm(double.Parse(Encoding.ASCII.GetString(Bytes(31)).TrimEnd('\0'), CultureInfo.InvariantCulture));
                case 100:
                case 115:
                case 118:
                case 119:
                    {
                        int n = tag is 115 or 119 ? Byte() : U16();
                        string name = tag is 118 or 119 ? new UTF8Encoding(false, true).GetString(Bytes(n)) : Encoding.Latin1.GetString(Bytes(n));
                        if (name.EnumerateRunes().Count() > 255)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return Term.A(name);
                    }
                case 106:
                    return Nil.Value;
                case 104:
                case 105:
                    {
                        int n = Count(tag == 104 ? Byte() : U32());
                        var items = new Term[n];
                        for (int i = 0; i < n; i++)
                            items[i] = Read(depth + 1);

                        return new TupleTerm(items);
                    }
                case 107:
                    {
                        int n = U16();

                        return Cons.From(Bytes(n).ToArray().Select(b => (Term)Term.I(b)));
                    }
                case 108:
                    {
                        int n = Count(U32());
                        var items = new Term[n];
                        for (int i = 0; i < n; i++)
                            items[i] = Read(depth + 1);

                        return Cons.From(items, Read(depth + 1));
                    }
                case 109:
                    {
                        int n = Count(U32());

                        return new BitString(Bytes(n));
                    }
                case 77:
                    {
                        int n = Count(U32());
                        int bits = Byte();
                        if (n == 0 || bits is < 1 or > 8)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return new BitString(Bytes(n), (n - 1) * 8 + bits);
                    }
                case 110:
                case 111:
                    {
                        int n = Count(tag == 110 ? Byte() : U32());
                        int sign = Byte();
                        if (sign > 1)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        var value = new BigInteger(Bytes(n), true, false);

                        return new Integer(sign == 1 ? -value : value);
                    }
                case 116:
                    {
                        int n = Count(U32(), 2);
                        var items = new KeyValuePair<Term, Term>[n];
                        for (int i = 0; i < n; i++)
                            items[i] = new(Read(depth + 1), Read(depth + 1));

                        return new MapTerm(items);
                    }
                case 88:
                case 103:
                    {
                        string node = Node(depth + 1);
                        uint id = U32(), serial = U32(), creation = tag == 88 ? U32() : Byte();

                        return new Pid(node, id | ((ulong)serial << 32), creation);
                    }
                case 120:
                case 89:
                case 102:
                    {
                        string node = Node(depth + 1);
                        ulong id = tag == 120 ? BinaryPrimitives.ReadUInt64BigEndian(Bytes(8)) : U32();
                        uint creation = tag == 102 ? Byte() : U32();

                        return new PortTerm(node, id, creation);
                    }
                case 90:
                case 114:
                    {
                        int n = U16();
                        string node = Node(depth + 1);
                        uint creation = tag == 90 ? U32() : Byte();
                        if (n is < 1 or > 2)
                            throw new NotSupportedException(EtfDiagnostics.LongReferenceId);
                        ulong id = U32();
                        if (n == 2)
                            id |= (ulong)U32() << 32;

                        return new ReferenceTerm(node, id, creation);
                    }
                case 101:
                    {
                        string node = Node(depth + 1);
                        uint id = U32();

                        return new ReferenceTerm(node, id, Byte());
                    }
                default:
                    throw new NotSupportedException(EtfDiagnostics.UnsupportedTag(tag));
            }
        }
    }
}
