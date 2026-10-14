using System.Numerics;

namespace Erlang;

public class ReferenceTerm : Term
{
    public string Node { get; }
    public ulong Id => checked((ulong)FullId);
    public BigInteger FullId { get; }
    public uint Creation { get; }
    public IReadOnlyList<uint> Words { get; }

    public ReferenceTerm(string node, ulong id, uint creation = 0) : this(node, id > uint.MaxValue ? [(uint)id, (uint)(id >> ReferenceIdentityLayout.WordBits)] : [(uint)id], creation)
    {
    }

    public ReferenceTerm(string node, IEnumerable<uint> words, uint creation = 0)
    {
        uint[] values = words.ToArray();
        if (values.Length < ReferenceIdentityLayout.MinimumWords || values.Length > ReferenceIdentityLayout.MaximumWords)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Node = node;
        Creation = creation;
        Words = Array.AsReadOnly(values);
        BigInteger id = BigInteger.Zero;
        for (int index = values.Length - 1; index >= 0; index--)
            id = (id << ReferenceIdentityLayout.WordBits) | values[index];
        FullId = id;
    }

    public override string ToString() => $"#Ref<{Node}.{FullId}.{Creation}>";
}
