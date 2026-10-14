using System.Buffers.Binary;
using System.Text;

namespace Erlang;

internal ref struct EtfWireBoundary(ReadOnlySpan<byte> data, uint? localSeed, Action<EtfNodeEpoch>? observe = null)
{
    private readonly ReadOnlySpan<byte> data = data;
    private int offset;
    private bool localNodes;

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
            case EtfTags.Record:
                {
                    int count = Count(U32(), EtfRecordLayout.TermsPerField);
                    Byte();
                    Terms(checked(count + EtfRecordLayout.IdentityTerms), depth);
                    break;
                }
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
                {
                    int start = offset;
                    Measure(depth + 1);
                    int end = offset;
                    Skip(EtfFieldWidths.UInt32Bytes * 2);
                    uint creation = tag == EtfTags.Pid ? Byte() : U32();
                    ObserveNode(
                        start,
                        end,
                        creation,
                        tag == EtfTags.Pid
                    );
                    break;
                }
            case EtfTags.Port:
            case EtfTags.NewPort:
            case EtfTags.V4Port:
                {
                    int start = offset;
                    Measure(depth + 1);
                    int end = offset;
                    Skip(tag == EtfTags.V4Port ? EtfFieldWidths.UInt64Bytes : EtfFieldWidths.UInt32Bytes);
                    uint creation = tag == EtfTags.Port ? Byte() : U32();
                    ObserveNode(
                        start,
                        end,
                        creation,
                        tag == EtfTags.Port
                    );
                    break;
                }
            case EtfTags.Reference:
                {
                    int start = offset;
                    Measure(depth + 1);
                    int end = offset;
                    Skip(EtfFieldWidths.UInt32Bytes);
                    ObserveNode(
                        start,
                        end,
                        Byte(),
                        true
                    );
                    break;
                }
            case EtfTags.NewReference:
            case EtfTags.NewerReference:
                {
                    int count = U16();
                    int start = offset;
                    Measure(depth + 1);
                    int end = offset;
                    uint creation = tag == EtfTags.NewReference ? Byte() : U32();
                    Skip(checked(count * EtfFieldWidths.UInt32Bytes));
                    ObserveNode(
                        start,
                        end,
                        creation,
                        tag == EtfTags.NewReference
                    );
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
                    localNodes = true;
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

    private void ObserveNode(
        int start,
        int end,
        uint creation,
        bool tinyCreation
    )
    {
        if (observe is null)
            return;
        if (tinyCreation && creation > EtfNodeLayout.MaximumTinyCreation)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ReadOnlySpan<byte> node = data.Slice(start, end - start);
        while (!node.IsEmpty && node[0] == EtfTags.Local)
            node = node[(EtfHeaderLayout.LocalTermOffset - EtfHeaderLayout.PlainPayloadOffset)..];
        if (node.Length == 1 && node[0] == EtfTags.Nil && localNodes)
        {
            observe(new(string.Empty, creation, true));

            return;
        }
        if (node.IsEmpty)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int tag = node[0];
        bool small = tag is EtfTags.SmallLatin1Atom or EtfTags.SmallUtf8Atom;
        if (!small && tag is not EtfTags.Latin1Atom and not EtfTags.Utf8Atom)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int header = 1 + (small ? 1 : EtfFieldWidths.UInt16Bytes);
        int length = small ? node[1] : BinaryPrimitives.ReadUInt16BigEndian(node[1..]);
        if (node.Length != header + length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        string name = (tag is EtfTags.SmallUtf8Atom or EtfTags.Utf8Atom ? EtfNodeEpochEncodings.StrictUtf8 : Encoding.Latin1).GetString(node[header..]);
        if (name.EnumerateRunes().Count() > EtfAtomLimits.MaximumCodePoints)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        observe(new(name, creation, false));
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
