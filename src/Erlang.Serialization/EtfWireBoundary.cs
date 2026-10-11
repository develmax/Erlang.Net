using System.Buffers.Binary;

namespace Erlang;

internal ref struct EtfWireBoundary(ReadOnlySpan<byte> data, uint? localSeed)
{
    private readonly ReadOnlySpan<byte> data = data;
    private int offset;

    public int Measure(int depth = 0)
    {
        if (depth > EtfResourceLimits.MaximumNestingDepth)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        int tag = Byte();
        switch (tag)
        {
            case EtfTags.SmallInteger:
                Skip(1);
                break;
            case EtfTags.Integer:
                Skip(EtfFieldWidths.UInt32Bytes);
                break;
            case EtfTags.NewFloat:
                Skip(EtfFloatLayout.Binary64Bytes);
                break;
            case EtfTags.LegacyFloat:
                Skip(EtfFloatLayout.LegacyTextBytes);
                break;
            case EtfTags.SmallLatin1Atom:
            case EtfTags.SmallUtf8Atom:
                Skip(Byte());
                break;
            case EtfTags.Latin1Atom:
            case EtfTags.Utf8Atom:
            case EtfTags.String:
                Skip(U16());
                break;
            case EtfTags.Nil:
                break;
            case EtfTags.SmallTuple:
                Terms(Byte(), depth);
                break;
            case EtfTags.LargeTuple:
                Terms(Count(U32()), depth);
                break;
            case EtfTags.List:
                Terms(Count(U32()), depth);
                Measure(depth + 1);
                break;
            case EtfTags.Map:
                Terms(Count(U32(), EtfMapLayout.TermsPerEntry), depth);
                break;
            case EtfTags.Binary:
                Skip(Count(U32()));
                break;
            case EtfTags.BitBinary:
                {
                    int count = Count(U32());
                    Byte();
                    Skip(count);
                    break;
                }
            case EtfTags.SmallBig:
            case EtfTags.LargeBig:
                {
                    int count = tag == EtfTags.SmallBig ? Byte() : Count(U32());
                    Byte();
                    Skip(count);
                    break;
                }
            case EtfTags.Pid:
            case EtfTags.NewPid:
                Measure(depth + 1);
                Skip(EtfFieldWidths.UInt32Bytes * 2 + (tag == EtfTags.Pid ? 1 : EtfFieldWidths.UInt32Bytes));
                break;
            case EtfTags.Port:
            case EtfTags.NewPort:
            case EtfTags.V4Port:
                Measure(depth + 1);
                Skip((tag == EtfTags.V4Port ? EtfFieldWidths.UInt64Bytes : EtfFieldWidths.UInt32Bytes) + (tag == EtfTags.Port ? 1 : EtfFieldWidths.UInt32Bytes));
                break;
            case EtfTags.Reference:
                Measure(depth + 1);
                Skip(EtfFieldWidths.UInt32Bytes + 1);
                break;
            case EtfTags.NewReference:
            case EtfTags.NewerReference:
                {
                    int count = U16();
                    Measure(depth + 1);
                    Skip(tag == EtfTags.NewReference ? 1 : EtfFieldWidths.UInt32Bytes);
                    Skip(checked(count * EtfFieldWidths.UInt32Bytes));
                    break;
                }
            case EtfTags.Export:
                Terms(3, depth);
                break;
            case EtfTags.NewFunction:
                {
                    Skip(EtfFunctionLayout.SizeBytes + 1 + EtfFunctionLayout.UniqueBytes + EtfFieldWidths.UInt32Bytes);
                    int count = Count(U32());
                    Terms(checked(count + 4), depth);
                    break;
                }
            case EtfTags.Local:
                {
                    uint expected = U32();
                    int start = offset;
                    Measure(depth);
                    if (localSeed is null || expected != ErlangBlockHash.Hash(data.Slice(start, offset - start), localSeed.Value))
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    break;
                }
            default:
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return offset;
    }

    private void Terms(int count, int depth)
    {
        if (count > data.Length - offset)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        for (int at = 0; at < count; at++)
            Measure(depth + 1);
    }

    private int Count(uint count, int multiplier = 1)
    {
        ulong total = (ulong)count * (uint)multiplier;
        if (total > (ulong)(data.Length - offset))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (int)total;
    }

    private byte Byte()
    {
        if (offset >= data.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

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
        if (count < 0 || count > data.Length - offset)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        offset += count;
    }
}
