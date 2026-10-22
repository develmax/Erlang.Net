using System.Buffers.Binary;
namespace Erlang;
// Source-specific size-state pass; no Term construction, node admission or callbacks.
internal ref struct EtfDistSizeBoundary423(ReadOnlySpan<byte> data, uint? localSeed)
{
    private readonly ReadOnlySpan<byte> data = data;
    private int offset;
    private long terms = 1;
    private int atomExtraSkip;
    private int localStart = -1;
    private long localTermEnd = -1;
    private uint expectedHash;

    internal int Measure()
    {
        while (terms != 0)
        {
            int tag = Byte();
            switch (tag)
            {
                case EtfTags.SmallInteger: Skip(1); break;
                case EtfTags.Integer: Skip(EtfFieldWidths.UInt32Bytes); break;
                case EtfTags.SmallBig:
                    {
                        int n = Byte();
                        Skip(checked(n + 1));
                        break;
                    }
                case EtfTags.LargeBig:
                    {
                        int n = Count(U32());
                        Skip(checked(n + 1));
                        break;
                    }
                case EtfTags.Latin1Atom:
                case EtfTags.Utf8Atom: AtomBytes(U16()); break;
                case EtfTags.SmallLatin1Atom:
                case EtfTags.SmallUtf8Atom: AtomBytes(Byte()); break;
                case EtfTags.AtomCacheReference: AtomBytes(1); break;
                case EtfTags.InternalAtomReference2: AtomBytes(EtfFieldWidths.UInt16Bytes); break;
                case EtfTags.InternalAtomReference3: AtomBytes(EtfDistScanLayout423.InternalAtom3Bytes); break;
                case EtfTags.NewPid: atomExtraSkip = EtfDistScanLayout423.PidTail; Add(1); break;
                case EtfTags.Pid: atomExtraSkip = EtfDistScanLayout423.LegacyPidTail; Add(1); break;
                case EtfTags.V4Port: atomExtraSkip = EtfDistScanLayout423.WidePortTail; Add(1); break;
                case EtfTags.NewPort: atomExtraSkip = EtfDistScanLayout423.PortTail; Add(1); break;
                case EtfTags.Port: atomExtraSkip = EtfDistScanLayout423.LegacyPortTail; Add(1); break;
                case EtfTags.NewerReference:
                case EtfTags.NewReference:
                    {
                        int count = U16();
                        if (count > EtfReferenceLayout.MaximumSupportedWords) Bad();
                        atomExtraSkip = checked(count * EtfFieldWidths.UInt32Bytes + (tag == EtfTags.NewReference ? 1 : EtfFieldWidths.UInt32Bytes));
                        Add(1);
                        break;
                    }
                case EtfTags.Reference: atomExtraSkip = EtfDistScanLayout423.LegacyReferenceTail; Add(1); break;
                case EtfTags.Nil:
                    if (atomExtraSkip != 0)
                    {
                        if (localStart < 0) Bad();
                        AtomBytes(0);
                    }
                    break;
                case EtfTags.List: Add(Count(U32())); Add(1); break;
                case EtfTags.SmallTuple: Add(Byte()); break;
                case EtfTags.LargeTuple:
                    {
                        int count = Count(U32());
                        if (count > EtfDistScanLayout423.MaximumTupleArity) Bad();
                        Add(count);
                        break;
                    }
                case EtfTags.Map:
                    {
                        int n = Count(U32());
                        Add(n);
                        Add(n);
                        break;
                    }
                case EtfTags.String: Skip(U16()); break;
                case EtfTags.LegacyFloat: Skip(EtfFloatLayout.LegacyTextBytes); break;
                case EtfTags.NewFloat: Skip(EtfFloatLayout.Binary64Bytes); break;
                case EtfTags.Binary: Skip(Count(U32())); break;
                case EtfTags.BitBinary:
                    {
                        int n = Count(U32());
                        Skip(checked(n + 1));
                        break;
                    }
                case EtfTags.Export: Add(3); break;
                case EtfTags.NewFunction:
                    {
                        Skip(EtfDistScanLayout423.NewFunctionPrefix);
                        int count = Count(U32());
                        if (count > EtfFunctionLayout.MaximumEnvironment) Bad();
                        Add(checked(count + 4));
                        break;
                    }
                case EtfTags.Local:
                    expectedHash = U32();
                    localStart = offset;
                    localTermEnd = terms - 1;
                    Add(1);
                    break;
                case EtfTags.Record:
                    {
                        int count = Count(U32());
                        Byte();
                        Add(checked(count * EtfRecordLayout.TermsPerField + EtfRecordLayout.IdentityTerms));
                        break;
                    }
                default: Bad(); break;
            }
            terms--;
            if (terms == localTermEnd)
            {
                int length = offset - localStart;
                if (length <= 0 || localSeed is null || ErlangBlockHash.Hash(data.Slice(localStart, length), localSeed.Value) != expectedHash) Bad();
                localStart = -1;
                localTermEnd = -1;
            }
        }

        return offset;
    }

    private void AtomBytes(int count)
    {
        Skip(checked(count + atomExtraSkip));
        atomExtraSkip = 0;
    }

    private void Add(int count)
    {
        if (count < 0 || (long)count + terms > (long)data.Length + 1) Bad();
        terms += count;
    }

    private int Count(uint count)
    {
        if (count > int.MaxValue || count > data.Length - offset) Bad();

        return (int)count;
    }

    private byte Byte()
    {
        if (offset >= data.Length) Bad();

        return data[offset++];
    }

    private int U16()
    {
        int start = offset;
        Skip(EtfFieldWidths.UInt16Bytes);

        return BinaryPrimitives.ReadUInt16BigEndian(data[start..]);
    }

    private uint U32()
    {
        int start = offset;
        Skip(EtfFieldWidths.UInt32Bytes);

        return BinaryPrimitives.ReadUInt32BigEndian(data[start..]);
    }

    private void Skip(int count)
    {
        if (count < 0 || count > data.Length - offset) Bad();
        offset += count;
    }

    private static void Bad() => throw new ErlangException(ErlangErrorReasons.BadArgument);
}
