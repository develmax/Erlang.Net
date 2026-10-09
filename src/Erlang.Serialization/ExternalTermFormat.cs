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
        stream.WriteByte(EtfHeaderLayout.Version);
        Write(stream, term, 0);

        return stream.ToArray();
    }

    public static Term Decode(ReadOnlySpan<byte> data, int maxBytes = DefaultMaxBytes)
    {
        if (data.Length > maxBytes || data.Length < EtfHeaderLayout.MinimumPacketBytes || data[EtfHeaderLayout.VersionOffset] != EtfHeaderLayout.Version)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        try
        {
            if (data[EtfHeaderLayout.TagOffset] == EtfTags.Compressed)
            {
                if (data.Length < EtfHeaderLayout.MinimumCompressedPacketBytes)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                uint expected = BinaryPrimitives.ReadUInt32BigEndian(data[EtfHeaderLayout.CompressedSizeOffset..]);
                if (expected > maxBytes)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                using var input = new MemoryStream(data[EtfHeaderLayout.CompressedPayloadOffset..].ToArray());
                using var z = new ZLibStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                byte[] buffer = new byte[EtfCompression.BufferBytes];
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
            var reader = new Reader(data[EtfHeaderLayout.PlainPayloadOffset..], maxBytes);
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
        Span<byte> b = stackalloc byte[EtfFieldWidths.UInt16Bytes];
        BinaryPrimitives.WriteUInt16BigEndian(b, checked((ushort)value));
        s.Write(b);
    }

    private static void U32(Stream s, uint value)
    {
        Span<byte> b = stackalloc byte[EtfFieldWidths.UInt32Bytes];
        BinaryPrimitives.WriteUInt32BigEndian(b, value);
        s.Write(b);
    }

    private static void Write(Stream s, Term term, int depth)
    {
        if (depth > EtfResourceLimits.MaximumNestingDepth)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        switch (term)
        {
            case Atom a:
                {
                    byte[] b = Encoding.UTF8.GetBytes(a.Name);
                    if (a.Name.EnumerateRunes().Count() > EtfAtomLimits.MaximumCodePoints)
                        throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    s.WriteByte(EtfTags.Utf8Atom);
                    U16(s, b.Length);
                    s.Write(b);
                    break;
                }
            case Integer i when i.Value >= 0 && i.Value <= EtfIntegerLayout.SmallIntegerMaximum:
                s.WriteByte(EtfTags.SmallInteger);
                s.WriteByte((byte)i.Value);
                break;
            case Integer i when i.Value >= int.MinValue && i.Value <= int.MaxValue:
                s.WriteByte(EtfTags.Integer);
                U32(s, unchecked((uint)(int)i.Value));
                break;
            case Integer i:
                {
                    byte[] b = BigInteger.Abs(i.Value).ToByteArray(true, false);
                    if (b.Length < EtfIntegerLayout.LargeBigMinimumDigits)
                    {
                        s.WriteByte(EtfTags.SmallBig);
                        s.WriteByte((byte)b.Length);
                    }
                    else
                    {
                        s.WriteByte(EtfTags.LargeBig);
                        U32(s, (uint)b.Length);
                    }
                    s.WriteByte((byte)(i.Value.Sign < 0 ? EtfIntegerLayout.NegativeSign : EtfIntegerLayout.NonNegativeSign));
                    s.Write(b);
                    break;
                }
            case FloatTerm f:
                {
                    s.WriteByte(EtfTags.NewFloat);
                    Span<byte> b = stackalloc byte[EtfFloatLayout.Binary64Bytes];
                    BinaryPrimitives.WriteInt64BigEndian(b, BitConverter.DoubleToInt64Bits(f.Value));
                    s.Write(b);
                    break;
                }
            case Nil:
                s.WriteByte(EtfTags.Nil);
                break;
            case TupleTerm t:
                if (t.Items.Count < EtfTupleLayout.LargeTupleMinimumArity)
                {
                    s.WriteByte(EtfTags.SmallTuple);
                    s.WriteByte((byte)t.Items.Count);
                }
                else
                {
                    s.WriteByte(EtfTags.LargeTuple);
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
                    s.WriteByte(EtfTags.List);
                    U32(s, (uint)items.Count);
                    foreach (var item in items)
                        Write(s, item, depth + 1);
                    Write(s, tail, depth + 1);
                    break;
                }
            case MapTerm m:
                s.WriteByte(EtfTags.Map);
                U32(s, (uint)m.Entries.Count);
                foreach (var e in m.Entries)
                {
                    Write(s, e.Key, depth + 1);
                    Write(s, e.Value, depth + 1);
                }
                break;
            case BitString b:
                s.WriteByte((byte)(b.IsBinary ? EtfTags.Binary : EtfTags.BitBinary));
                byte[] data = b.ToArray();
                U32(s, (uint)data.Length);
                if (!b.IsBinary)
                    s.WriteByte((byte)(b.BitLength % EtfBitBinaryLayout.BitsPerByte));
                s.Write(data);
                break;
            case Pid p:
                s.WriteByte(EtfTags.NewPid);
                Write(s, Term.A(p.Node), depth + 1);
                U32(s, (uint)p.Id);
                U32(s, (uint)(p.Id >> EtfPidLayout.SerialBitShift));
                U32(s, p.Creation);
                break;
            case PortTerm p:
                s.WriteByte(EtfTags.V4Port);
                Write(s, Term.A(p.Node), depth + 1);
                Span<byte> id = stackalloc byte[EtfFieldWidths.UInt64Bytes];
                BinaryPrimitives.WriteUInt64BigEndian(id, p.Id);
                s.Write(id);
                U32(s, p.Creation);
                break;
            case ReferenceTerm r:
                s.WriteByte(EtfTags.NewerReference);
                U16(s, r.Id > uint.MaxValue ? EtfReferenceLayout.DoubleIdWords : EtfReferenceLayout.SingleIdWord);
                Write(s, Term.A(r.Node), depth + 1);
                U32(s, r.Creation);
                U32(s, (uint)r.Id);
                if (r.Id > uint.MaxValue)
                    U32(s, (uint)(r.Id >> EtfReferenceLayout.IdWordBitShift));
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

        private int U16() => BinaryPrimitives.ReadUInt16BigEndian(Bytes(EtfFieldWidths.UInt16Bytes));

        private uint U32() => BinaryPrimitives.ReadUInt32BigEndian(Bytes(EtfFieldWidths.UInt32Bytes));

        private int Count(uint n, int minimum = 1)
        {
            if (n > limit || n > int.MaxValue || n * (ulong)minimum > (ulong)(data.Length - offset))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return (int)n;
        }

        private string Node(int depth) => Read(depth) is Atom a ? a.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);

        public Term Read(int depth)
        {
            if (depth > EtfResourceLimits.MaximumNestingDepth)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            int tag = Byte();
            switch (tag)
            {
                case EtfTags.SmallInteger:
                    return Term.I(Byte());
                case EtfTags.Integer:
                    return Term.I(unchecked((int)U32()));
                case EtfTags.NewFloat:
                    return new FloatTerm(BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(Bytes(EtfFloatLayout.Binary64Bytes))));
                case EtfTags.LegacyFloat:
                    return new FloatTerm(double.Parse(Encoding.ASCII.GetString(Bytes(EtfFloatLayout.LegacyTextBytes)).TrimEnd('\0'), CultureInfo.InvariantCulture));
                case EtfTags.Latin1Atom:
                case EtfTags.SmallLatin1Atom:
                case EtfTags.Utf8Atom:
                case EtfTags.SmallUtf8Atom:
                    {
                        int n = tag is EtfTags.SmallLatin1Atom or EtfTags.SmallUtf8Atom ? Byte() : U16();
                        string name = tag is EtfTags.Utf8Atom or EtfTags.SmallUtf8Atom ? new UTF8Encoding(false, true).GetString(Bytes(n)) : Encoding.Latin1.GetString(Bytes(n));
                        if (name.EnumerateRunes().Count() > EtfAtomLimits.MaximumCodePoints)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return Term.A(name);
                    }
                case EtfTags.Nil:
                    return Nil.Value;
                case EtfTags.SmallTuple:
                case EtfTags.LargeTuple:
                    {
                        int n = Count(tag == EtfTags.SmallTuple ? Byte() : U32());
                        var items = new Term[n];
                        for (int i = 0; i < n; i++)
                            items[i] = Read(depth + 1);

                        return new TupleTerm(items);
                    }
                case EtfTags.String:
                    {
                        int n = U16();

                        return Cons.From(Bytes(n).ToArray().Select(b => (Term)Term.I(b)));
                    }
                case EtfTags.List:
                    {
                        int n = Count(U32());
                        var items = new Term[n];
                        for (int i = 0; i < n; i++)
                            items[i] = Read(depth + 1);

                        return Cons.From(items, Read(depth + 1));
                    }
                case EtfTags.Binary:
                    {
                        int n = Count(U32());

                        return new BitString(Bytes(n));
                    }
                case EtfTags.BitBinary:
                    {
                        int n = Count(U32());
                        int bits = Byte();
                        if (n == 0 || bits is < EtfBitBinaryLayout.MinimumTailBits or > EtfBitBinaryLayout.MaximumTailBits)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return new BitString(Bytes(n), (n - 1) * EtfBitBinaryLayout.BitsPerByte + bits);
                    }
                case EtfTags.SmallBig:
                case EtfTags.LargeBig:
                    {
                        int n = Count(tag == EtfTags.SmallBig ? Byte() : U32());
                        int sign = Byte();
                        if (sign > EtfIntegerLayout.NegativeSign)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        var value = new BigInteger(Bytes(n), true, false);

                        return new Integer(sign == EtfIntegerLayout.NegativeSign ? -value : value);
                    }
                case EtfTags.Map:
                    {
                        int n = Count(U32(), EtfMapLayout.TermsPerEntry);
                        var items = new KeyValuePair<Term, Term>[n];
                        for (int i = 0; i < n; i++)
                            items[i] = new(Read(depth + 1), Read(depth + 1));

                        return new MapTerm(items);
                    }
                case EtfTags.NewPid:
                case EtfTags.Pid:
                    {
                        string node = Node(depth + 1);
                        uint id = U32(), serial = U32(), creation = tag == EtfTags.NewPid ? U32() : Byte();

                        return new Pid(node, id | ((ulong)serial << EtfPidLayout.SerialBitShift), creation);
                    }
                case EtfTags.V4Port:
                case EtfTags.NewPort:
                case EtfTags.Port:
                    {
                        string node = Node(depth + 1);
                        ulong id = tag == EtfTags.V4Port ? BinaryPrimitives.ReadUInt64BigEndian(Bytes(EtfFieldWidths.UInt64Bytes)) : U32();
                        uint creation = tag == EtfTags.Port ? Byte() : U32();

                        return new PortTerm(node, id, creation);
                    }
                case EtfTags.NewerReference:
                case EtfTags.NewReference:
                    {
                        int n = U16();
                        string node = Node(depth + 1);
                        uint creation = tag == EtfTags.NewerReference ? U32() : Byte();
                        if (n is < EtfReferenceLayout.MinimumSupportedWords or > EtfReferenceLayout.MaximumSupportedWords)
                            throw new NotSupportedException(EtfDiagnostics.LongReferenceId);
                        ulong id = U32();
                        if (n == EtfReferenceLayout.DoubleIdWords)
                            id |= (ulong)U32() << EtfReferenceLayout.IdWordBitShift;

                        return new ReferenceTerm(node, id, creation);
                    }
                case EtfTags.Reference:
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
