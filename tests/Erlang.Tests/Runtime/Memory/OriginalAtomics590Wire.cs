using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.OriginalAtomics590Identity;

namespace Erlang.Tests;

// Bounded wire traversal, not an encoder/normalizer. Whole outside spans remain
// literally compared, including map wire order, tags, lengths and all frames.
internal sealed class OriginalAtomics590Wire(byte[] data, ReferenceTerm role)
{
    private int position, terms;
    private readonly List<int> words = new();

    internal IReadOnlyList<int> Read()
    {
        Require(data.Length > OriginalAtomics590WireNames.VersionBytes && data.Length <= OriginalAtomics590WireNames.MaxEncodedBytes && Byte() == OriginalAtomics590WireNames.Version);
        Term(0);
        Require(position == data.Length);

        return words;
    }

    private byte Byte()
    {
        Require(position < data.Length);

        return data[position++];
    }

    private int U16()
    {
        int n = Byte();

        return (n << OriginalAtomics590WireNames.ByteShift) | Byte();
    }

    private int Count()
    {
        uint n = U32();
        Require(n <= OriginalAtomics590WireNames.MaxCollectionLength);

        return (int)n;
    }

    private uint U32()
    {
        Require(position <= data.Length - OriginalAtomics590WireNames.UInt32Bytes);
        uint n = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position, OriginalAtomics590WireNames.UInt32Bytes));
        position += OriginalAtomics590WireNames.UInt32Bytes;

        return n;
    }

    private void Skip(int count)
    {
        Require(count >= 0 && position <= data.Length - count);
        position += count;
    }

    private void Children(int count, int depth)
    {
        Require(count >= 0 && count <= OriginalAtomics590WireNames.MaxTerms - terms);
        for (int i = 0; i < count; i++) Term(depth + 1);
    }

    private string Atom()
    {
        int tag = Byte();
        Require(tag is OriginalAtomics590WireNames.AtomExt or OriginalAtomics590WireNames.SmallAtomExt or OriginalAtomics590WireNames.AtomUtf8Ext or OriginalAtomics590WireNames.SmallAtomUtf8Ext);
        int length = tag is OriginalAtomics590WireNames.SmallAtomExt or OriginalAtomics590WireNames.SmallAtomUtf8Ext ? Byte() : U16();
        Require(position <= data.Length - length);
        string text = (tag is OriginalAtomics590WireNames.AtomUtf8Ext or OriginalAtomics590WireNames.SmallAtomUtf8Ext ? new UTF8Encoding(false, true) : Encoding.Latin1).GetString(data, position, length);
        position += length;

        return text;
    }

    private void Term(int depth)
    {
        Require(depth <= OriginalAtomics590WireNames.MaxDepth && ++terms <= OriginalAtomics590WireNames.MaxTerms);
        int tag = Byte();
        switch (tag)
        {
            case OriginalAtomics590WireNames.SmallIntegerExt: Skip(OriginalAtomics590WireNames.SmallIntegerBytes); break;
            case OriginalAtomics590WireNames.IntegerExt: Skip(OriginalAtomics590WireNames.IntegerBytes); break;
            case OriginalAtomics590WireNames.NewFloatExt: Skip(OriginalAtomics590WireNames.NewFloatBytes); break;
            case OriginalAtomics590WireNames.FloatExt: Skip(OriginalAtomics590WireNames.OldFloatBytes); break;
            case OriginalAtomics590WireNames.AtomExt: case OriginalAtomics590WireNames.AtomUtf8Ext: case OriginalAtomics590WireNames.StringExt: Skip(U16()); break;
            case OriginalAtomics590WireNames.SmallAtomExt: case OriginalAtomics590WireNames.SmallAtomUtf8Ext: Skip(Byte()); break;
            case OriginalAtomics590WireNames.NilExt: break;
            case OriginalAtomics590WireNames.SmallTupleExt: Children(Byte(), depth); break;
            case OriginalAtomics590WireNames.LargeTupleExt: Children(Count(), depth); break;
            case OriginalAtomics590WireNames.ListExt:
                {
                    int count = Count();
                    Children(count, depth);
                    Term(depth + 1);
                    break;
                }
            case OriginalAtomics590WireNames.MapExt: Children(checked(Count() * OriginalAtomics590WireNames.MapEntryTerms), depth); break;
            case OriginalAtomics590WireNames.BinaryExt: Skip(Count()); break;
            case OriginalAtomics590WireNames.BitBinaryExt:
                {
                    int count = Count();
                    Skip(checked(count + OriginalAtomics590WireNames.BitBinarySizeByte));
                    break;
                }
            case OriginalAtomics590WireNames.SmallBigExt:
                {
                    int count = Byte();
                    Skip(checked(count + OriginalAtomics590WireNames.BigSignByte));
                    break;
                }
            case OriginalAtomics590WireNames.LargeBigExt:
                {
                    int count = Count();
                    Skip(checked(count + OriginalAtomics590WireNames.BigSignByte));
                    break;
                }
            case OriginalAtomics590WireNames.NewerReferenceExt:
                {
                    Require(U16() == OriginalAtomics590WireNames.ExpectedReferenceWords);
                    string node = Atom();
                    uint creation = U32();
                    int start = position;
                    uint[] actual = [U32(), U32(), U32()];
                    Require(node == role.Node && creation == role.Creation && actual.SequenceEqual(role.Words));
                    // Skip only the twelve opaque number bytes. Tag/count/node/
                    // creation still belong to the exact byte-comparison chunks.
                    words.Add(start);
                    break;
                }
            default: throw new InvalidDataException(OriginalAtomics590WireNames.UnexpectedTagPrefix + tag);
        }
    }
}
