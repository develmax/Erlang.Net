using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;

namespace Erlang;

public static class ExternalTermFormat
{
    public const int DefaultMaxBytes = 16 * 1024 * 1024;

    public static byte[] Encode(Term term) => Encode(term, new EtfEncodingOptions());

    public static byte[] Encode(Term term, EtfEncodingOptions options)
    {
        Validate(options);
        using var stream = new MemoryStream();
        stream.WriteByte(EtfHeaderLayout.Version);
        if (options.Local)
        {
            stream.WriteByte(EtfTags.Local);
            U32(stream, 0);
        }
        Write(
            stream,
            term,
            0,
            options
        );
        var encoded = stream.ToArray();
        if (options.Local)
            BinaryPrimitives.WriteUInt32BigEndian(
                encoded.AsSpan(EtfHeaderLayout.LocalHashOffset),
                ErlangBlockHash.Hash(encoded.AsSpan(EtfHeaderLayout.LocalTermOffset), options.LocalSeed)
            );
        if (options.CompressionLevel == EtfEncodingParameters.Uncompressed)
            return encoded;
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, new ZLibCompressionOptions { CompressionLevel = options.CompressionLevel }, true))
            zlib.Write(encoded.AsSpan(EtfHeaderLayout.PlainPayloadOffset));
        if (compressed.Length + EtfHeaderLayout.CompressedPayloadOffset > encoded.Length)
            return encoded;
        using var packet = new MemoryStream();
        packet.WriteByte(EtfHeaderLayout.Version);
        packet.WriteByte(EtfTags.Compressed);
        U32(packet, (uint)(encoded.Length - EtfHeaderLayout.PlainPayloadOffset));
        compressed.Position = 0;
        compressed.CopyTo(packet);

        return packet.ToArray();
    }

    private static void Validate(EtfEncodingOptions options)
    {
        if (options.MinorVersion < EtfEncodingParameters.MinimumMinorVersion || options.MinorVersion > EtfEncodingParameters.MaximumMinorVersion || options.CompressionLevel < EtfEncodingParameters.MinimumCompressionLevel || options.CompressionLevel > EtfEncodingParameters.MaximumCompressionLevel || options.Local && (options.Deterministic || options.LocalNode is null))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    public static long EncodedSize(Term term, EtfEncodingOptions options)
    {
        Validate(options);
        using var stream = new EtfSizeStream();
        stream.WriteByte(EtfHeaderLayout.Version);
        if (options.Local)
        {
            stream.WriteByte(EtfTags.Local);
            U32(stream, 0);
        }
        Write(
            stream,
            term,
            0,
            options
        );

        return stream.Length;
    }

    public static IReadOnlyList<BitString> EncodeIoVec(Term term, EtfEncodingOptions options)
    {
        Validate(options);
        if (options.CompressionLevel != EtfEncodingParameters.Uncompressed || options.Local)
            return Array.AsReadOnly(new[] { BitString.FromOwnedBytes(Encode(term, options)) });
        using var stream = new EtfIoVecStream();
        stream.WriteByte(EtfHeaderLayout.Version);
        Write(
            stream,
            term,
            0,
            options
        );

        return stream.Finish();
    }

    public static Term Decode(ReadOnlySpan<byte> data, int maxBytes = DefaultMaxBytes)
        => DecodePacket(
            data,
            new EtfDecodeOptions(),
            maxBytes,
            true
        ).Term;

    public static EtfDecodedTerm DecodeOne(ReadOnlySpan<byte> data, EtfDecodeOptions options, int maxBytes = DefaultMaxBytes)
        => DecodePacket(
            data,
            options,
            maxBytes,
            false
        );

    private static EtfDecodedTerm DecodePacket(
        ReadOnlySpan<byte> data,
        EtfDecodeOptions options,
        int maxBytes,
        bool strict
    )
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
                int compressedBytes = ZlibMemberBoundary.Measure(data[EtfHeaderLayout.CompressedPayloadOffset..], (int)expected);
                int used = EtfHeaderLayout.CompressedPayloadOffset + compressedBytes;
                if (strict && used != data.Length)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                using var input = new MemoryStream(data.Slice(EtfHeaderLayout.CompressedPayloadOffset, compressedBytes).ToArray());
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
                var compressedBoundary = new EtfWireBoundary(bytes, options.LocalSeed);
                compressedBoundary.Measure();
                var compressedReader = new Reader(bytes, maxBytes, options);
                var result = compressedReader.Read(0);
                if (strict && !compressedReader.AtEnd)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return new EtfDecodedTerm(result, used);
            }
            var boundary = new EtfWireBoundary(data[EtfHeaderLayout.PlainPayloadOffset..], options.LocalSeed);
            boundary.Measure();
            var reader = new Reader(data[EtfHeaderLayout.PlainPayloadOffset..], maxBytes, options);
            var term = reader.Read(0);
            if (strict && !reader.AtEnd)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return new EtfDecodedTerm(term, EtfHeaderLayout.PlainPayloadOffset + reader.Position);
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

    private static bool WriteNode(
        Stream stream,
        string node,
        uint creation,
        int depth,
        EtfEncodingOptions options
    )
    {
        bool local = options.Local && node == options.LocalNode && creation == options.LocalCreation;
        Write(
            stream,
            local ? Nil.Value : Term.A(node),
            depth,
            options
        );

        return local;
    }

    private static void Write(
        Stream s,
        Term term,
        int depth,
        EtfEncodingOptions options
    )
    {
        if (depth > EtfResourceLimits.MaximumNestingDepth)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        switch (term)
        {
            case Atom a:
                {
                    bool utf8 = options.MinorVersion == EtfEncodingParameters.DefaultMinorVersion || a.Name.EnumerateRunes().Any(rune => rune.Value > byte.MaxValue);
                    byte[] b = (utf8 ? Encoding.UTF8 : Encoding.Latin1).GetBytes(a.Name);
                    if (a.Name.EnumerateRunes().Count() > EtfAtomLimits.MaximumCodePoints)
                        throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    bool small = utf8 && b.Length <= EtfEncodingParameters.MaximumSmallAtomBytes;
                    s.WriteByte(utf8 ? small ? EtfTags.SmallUtf8Atom : EtfTags.Utf8Atom : EtfTags.Latin1Atom);
                    if (small)
                        s.WriteByte((byte)b.Length);
                    else
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
                    if (options.MinorVersion == EtfEncodingParameters.MinimumMinorVersion)
                    {
                        s.WriteByte(EtfTags.LegacyFloat);
                        byte[] text = new byte[EtfFloatLayout.LegacyTextBytes];
                        Encoding.ASCII.GetBytes(ErlangFloatScientific.Format(f.Value, EtfEncodingParameters.LegacyFloatPrecision), text);
                        s.Write(text);
                        break;
                    }
                    s.WriteByte(EtfTags.NewFloat);
                    Span<byte> b = stackalloc byte[EtfFloatLayout.Binary64Bytes];
                    BinaryPrimitives.WriteInt64BigEndian(b, BitConverter.DoubleToInt64Bits(f.Value));
                    s.Write(b);
                    break;
                }
            case Nil:
                s.WriteByte(EtfTags.Nil);
                break;
            case NativeRecordTerm record:
                s.WriteByte(EtfTags.Record);
                U32(s, (uint)record.FieldNames.Count);
                s.WriteByte(record.IsExported ? EtfRecordLayout.Exported : EtfRecordLayout.Private);
                Write(
                    s,
                    record.Module,
                    depth + 1,
                    options
                );
                Write(
                    s,
                    record.Name,
                    depth + 1,
                    options
                );
                foreach (var field in record.FieldNames)
                    Write(
                        s,
                        field,
                        depth + 1,
                        options
                    );
                foreach (var value in record.Values)
                    Write(
                        s,
                        value,
                        depth + 1,
                        options
                    );
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
                    Write(
                        s,
                        item,
                        depth + 1,
                        options
                    );
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
                    if (tail is Nil && items.Count <= EtfEncodingParameters.MaximumStringLength && items.All(item => item is Integer integer && integer.Value >= 0 && integer.Value <= EtfEncodingParameters.MaximumStringByte))
                    {
                        s.WriteByte(EtfTags.String);
                        U16(s, items.Count);
                        foreach (Integer item in items)
                            s.WriteByte((byte)item.Value);
                        break;
                    }
                    s.WriteByte(EtfTags.List);
                    U32(s, (uint)items.Count);
                    foreach (var item in items)
                        Write(
                            s,
                            item,
                            depth + 1,
                            options
                        );
                    Write(
                        s,
                        tail,
                        depth + 1,
                        options
                    );
                    break;
                }
            case MapTerm m:
                s.WriteByte(EtfTags.Map);
                U32(s, (uint)m.Entries.Count);
                foreach (var e in m.Entries)
                {
                    Write(
                        s,
                        e.Key,
                        depth + 1,
                        options
                    );
                    Write(
                        s,
                        e.Value,
                        depth + 1,
                        options
                    );
                }
                break;
            case BitString b:
                s.WriteByte((byte)(b.IsBinary ? EtfTags.Binary : EtfTags.BitBinary));
                U32(s, (uint)b.ByteLength);
                if (!b.IsBinary)
                    s.WriteByte((byte)(b.BitLength % EtfBitBinaryLayout.BitsPerByte));
                if (s is EtfSizeStream sizeStream)
                    sizeStream.Add(b.ByteLength);
                else if (s is EtfIoVecStream ioVecStream && b.ByteLength >= EtfIoVecLayout.MinimumBorrowedBinaryBytes)
                    ioVecStream.Append(b.ByteView());
                else
                    b.WriteTo(s);
                break;
            case FunctionTerm f when f.ExternalIdentity is not null:
                s.WriteByte(EtfTags.Export);
                Write(
                    s,
                    f.ExternalIdentity.Module,
                    depth + 1,
                    options
                );
                Write(
                    s,
                    f.ExternalIdentity.Function,
                    depth + 1,
                    options
                );
                Write(
                    s,
                    Term.I(f.Arity),
                    depth + 1,
                    options
                );
                break;
            case FunctionTerm f when f.LocalIdentity is not null:
                {
                    if (options.FunctionOwner is null || f.Arity < 0 || f.Arity > EtfFunctionLayout.MaximumArity || f.Environment.Count > EtfFunctionLayout.MaximumEnvironment)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    s.WriteByte(EtfTags.NewFunction);
                    long start = s.Position;
                    U32(s, 0);
                    s.WriteByte((byte)f.Arity);
                    byte[] unique = f.LocalIdentity.Unique.ToArray();
                    if (unique.Length != EtfFunctionLayout.UniqueBytes || !f.LocalIdentity.Unique.IsBinary)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    s.Write(unique);
                    U32(s, unchecked((uint)f.LocalIdentity.Index));
                    U32(s, (uint)f.Environment.Count);
                    Write(
                        s,
                        f.LocalIdentity.Module,
                        depth + 1,
                        options
                    );
                    Write(
                        s,
                        Term.I(f.LocalIdentity.OldIndex),
                        depth + 1,
                        options
                    );
                    Write(
                        s,
                        Term.I(f.LocalIdentity.OldUnique),
                        depth + 1,
                        options
                    );
                    Write(
                        s,
                        options.FunctionOwner,
                        depth + 1,
                        options
                    );
                    foreach (Term value in f.Environment)
                        Write(
                            s,
                            value,
                            depth + 1,
                            options
                        );
                    long end = s.Position;
                    s.Position = start;
                    U32(s, checked((uint)(end - start)));
                    s.Position = end;
                    break;
                }
            case Pid p:
                {
                    s.WriteByte(EtfTags.NewPid);
                    bool local = WriteNode(
                        s,
                        p.Node,
                        p.Creation,
                        depth + 1,
                        options
                    );
                    U32(s, (uint)p.Id);
                    U32(s, (uint)(p.Id >> EtfPidLayout.SerialBitShift));
                    U32(s, local ? 0 : p.Creation);
                    break;
                }
            case PortTerm p:
                {
                    s.WriteByte(EtfTags.V4Port);
                    bool local = WriteNode(
                        s,
                        p.Node,
                        p.Creation,
                        depth + 1,
                        options
                    );
                    Span<byte> id = stackalloc byte[EtfFieldWidths.UInt64Bytes];
                    BinaryPrimitives.WriteUInt64BigEndian(id, p.Id);
                    s.Write(id);
                    U32(s, local ? 0 : p.Creation);
                    break;
                }
            case ReferenceTerm r:
                {
                    if (r is IReferenceResourceSerialization resource)
                        resource.SaveReferenceIdentity();
                    s.WriteByte(EtfTags.NewerReference);
                    U16(s, r.Words.Count);
                    bool local = WriteNode(
                        s,
                        r.Node,
                        r.Creation,
                        depth + 1,
                        options
                    );
                    U32(s, local ? 0 : r.Creation);
                    foreach (uint word in r.Words)
                        U32(s, word);
                    break;
                }
            default:
                throw new NotSupportedException(EtfDiagnostics.UnsupportedEncoding(term.GetType().Name));
        }
    }

    private ref struct Reader(ReadOnlySpan<byte> data, int limit, EtfDecodeOptions options)
    {
        private readonly ReadOnlySpan<byte> data = data; private int offset;
        private bool localNodes;
        private bool internalNode;
        public bool AtEnd => offset == data.Length;
        public int Position => offset;

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

        private ReferenceTerm RestoreReference(ReferenceTerm reference)
        {
            ReferenceTerm restored = options.ReferenceFactory?.Invoke(reference) ?? reference;
            if (!restored.Equals(reference))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return restored;
        }

        private static int LowSignedWord(BigInteger value) => unchecked((int)(uint)(value & uint.MaxValue));

        private static FloatTerm Float(double value) => double.IsFinite(value) ? new FloatTerm(value) : throw new ErlangException(ErlangErrorReasons.BadArgument);

        private int Count(uint n, int minimum = 1)
        {
            if (n > limit || n > int.MaxValue || n * (ulong)minimum > (ulong)(data.Length - offset))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return (int)n;
        }

        private string Node(int depth)
        {
            Term term = Read(depth);
            internalNode = term is Nil && localNodes && options.LocalNode is not null;

            return term is Atom atom ? atom.Name : internalNode ? options.LocalNode! : throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        private uint Creation(uint value) => internalNode ? options.LocalCreation : value;

        public Term Read(int depth)
        {
            if (depth > EtfResourceLimits.MaximumNestingDepth)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            int tag = Byte();
            switch (tag)
            {
                case EtfTags.Local:
                    U32();
                    localNodes = true;

                    return Read(depth);
                case EtfTags.SmallInteger:
                    return Term.I(Byte());
                case EtfTags.Integer:
                    return Term.I(unchecked((int)U32()));
                case EtfTags.NewFloat:
                    return Float(BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(Bytes(EtfFloatLayout.Binary64Bytes))));
                case EtfTags.LegacyFloat:
                    return ErlangFloatText.Parse(Bytes(EtfFloatLayout.LegacyTextBytes).ToArray().Select(value => (int)value));
                case EtfTags.Latin1Atom:
                case EtfTags.SmallLatin1Atom:
                case EtfTags.Utf8Atom:
                case EtfTags.SmallUtf8Atom:
                    {
                        int n = tag is EtfTags.SmallLatin1Atom or EtfTags.SmallUtf8Atom ? Byte() : U16();
                        string name = tag is EtfTags.Utf8Atom or EtfTags.SmallUtf8Atom ? new UTF8Encoding(false, true).GetString(Bytes(n)) : Encoding.Latin1.GetString(Bytes(n));
                        if (name.EnumerateRunes().Count() > EtfAtomLimits.MaximumCodePoints)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        if (options.Safe && !AtomNameRegistry.Exists(name))
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return Term.A(name);
                    }
                case EtfTags.Nil:
                    return Nil.Value;
                case EtfTags.Record:
                    {
                        int count = Count(U32());
                        byte flags = Byte();
                        if (flags != EtfRecordLayout.Private && flags != EtfRecordLayout.Exported)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        if (Read(depth + 1) is not Atom module || Read(depth + 1) is not Atom name)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        var keys = new Atom[count];
                        for (int index = 0; index < count; index++)
                        {
                            if (Read(depth + 1) is not Atom key)
                                throw new ErlangException(ErlangErrorReasons.BadArgument);
                            keys[index] = key;
                        }
                        var fields = new KeyValuePair<Atom, Term>[count];
                        for (int index = 0; index < count; index++)
                            fields[index] = new(keys[index], Read(depth + 1));

                        return new NativeRecordTerm(
                            module,
                            name,
                            flags == EtfRecordLayout.Exported,
                            fields
                        );
                    }
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
                        var value = new BigInteger(Bytes(n), true, false);

                        return new Integer(sign != EtfIntegerLayout.NonNegativeSign ? -value : value);
                    }
                case EtfTags.Map:
                    {
                        int n = Count(U32(), EtfMapLayout.TermsPerEntry);
                        var items = new KeyValuePair<Term, Term>[n];
                        for (int i = 0; i < n; i++)
                            items[i] = new(Read(depth + 1), Read(depth + 1));

                        var map = new MapTerm(items);
                        if (map.Entries.Count != n)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return map;
                    }
                case EtfTags.Export:
                    {
                        if (Read(depth + 1) is not Atom module || Read(depth + 1) is not Atom function || Read(depth + 1) is not Integer arity || arity.Value < 0 || arity.Value > EtfFunctionLayout.MaximumArity || options.ExternalFunctionFactory is null)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return options.ExternalFunctionFactory(new ExternalFunctionIdentity(module, function, (int)arity.Value), options.Safe);
                    }
                case EtfTags.NewFunction:
                    {
                        U32();
                        int arity = Byte();
                        var unique = new BitString(Bytes(EtfFunctionLayout.UniqueBytes));
                        uint index = U32();
                        int count = Count(U32());
                        if (count > EtfFunctionLayout.MaximumEnvironment || Read(depth + 1) is not Atom module || Read(depth + 1) is not Integer oldIndex || oldIndex.Value < EtfFunctionLayout.MinimumSmall || oldIndex.Value > EtfFunctionLayout.MaximumSmall || Read(depth + 1) is not Integer oldUnique || oldUnique.Value < EtfFunctionLayout.MinimumSmall || oldUnique.Value > EtfFunctionLayout.MaximumSmall || Read(depth + 1) is not Pid || options.LocalFunctionFactory is null)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        var environment = new Term[count];
                        for (int at = 0; at < count; at++)
                            environment[at] = Read(depth + 1);

                        return options.LocalFunctionFactory(
                            new EtfLocalFunctionCode(
                                module,
                                unchecked((int)index),
                                LowSignedWord(oldIndex.Value),
                                LowSignedWord(oldUnique.Value),
                                unique
                            ),
                            arity,
                            Array.AsReadOnly(environment)
                        );
                    }
                case EtfTags.NewPid:
                case EtfTags.Pid:
                    {
                        string node = Node(depth + 1);
                        uint id = U32(), serial = U32(), creation = tag == EtfTags.NewPid ? U32() : Byte();
                        if (tag == EtfTags.Pid && creation > EtfNodeLayout.MaximumTinyCreation)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        creation = Creation(creation);
                        if (node == options.LocalNode && creation == options.LocalCreation && id > EtfNodeLayout.MaximumLocalPidNumber)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return new Pid(node, id | ((ulong)serial << EtfPidLayout.SerialBitShift), creation);
                    }
                case EtfTags.V4Port:
                case EtfTags.NewPort:
                case EtfTags.Port:
                    {
                        string node = Node(depth + 1);
                        ulong id = tag == EtfTags.V4Port ? BinaryPrimitives.ReadUInt64BigEndian(Bytes(EtfFieldWidths.UInt64Bytes)) : U32();
                        uint creation = tag == EtfTags.Port ? Byte() : U32();
                        if (tag == EtfTags.Port && creation > EtfNodeLayout.MaximumTinyCreation)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        creation = Creation(creation);
                        if (node == options.LocalNode && creation == options.LocalCreation && id > EtfNodeLayout.MaximumLocalPortNumber)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return new PortTerm(node, id, creation);
                    }
                case EtfTags.NewerReference:
                case EtfTags.NewReference:
                    {
                        int n = U16();
                        string node = Node(depth + 1);
                        uint creation = tag == EtfTags.NewerReference ? U32() : Byte();
                        if (tag == EtfTags.NewReference && creation > EtfNodeLayout.MaximumTinyCreation)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        creation = Creation(creation);
                        if (n is < EtfReferenceLayout.MinimumSupportedWords or > EtfReferenceLayout.MaximumSupportedWords)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        var words = new uint[n];
                        for (int index = 0; index < n; index++)
                            words[index] = U32();
                        if ((tag == EtfTags.NewReference || node == options.LocalNode && creation == options.LocalCreation) && words[0] > ReferenceIdentityLayout.LocalFirstWordMask)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);

                        return RestoreReference(new ReferenceTerm(node, words, creation));
                    }
                case EtfTags.Reference:
                    {
                        string node = Node(depth + 1);
                        uint id = U32();
                        uint creation = Byte();
                        if (id > ReferenceIdentityLayout.LocalFirstWordMask || creation > EtfNodeLayout.MaximumTinyCreation)
                            throw new ErlangException(ErlangErrorReasons.BadArgument);
                        creation = Creation(creation);

                        return RestoreReference(new ReferenceTerm(node, id, creation));
                    }
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }
}
