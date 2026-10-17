using System.Numerics;

namespace Erlang;

public class ReferenceTerm : Term
{
    private readonly LocalNodeIdentity? localIdentity;
    private readonly LocalNodeSnapshot externalIdentity;
    public string Node => WireIdentity.Node;
    internal LocalNodeSnapshot WireIdentity => localIdentity?.Snapshot ?? externalIdentity;
    internal LocalNodeIdentity? LocalIdentity => localIdentity;
    public ulong Id => checked((ulong)FullId);
    public BigInteger FullId { get; }
    public uint Creation => WireIdentity.Creation;
    public IReadOnlyList<uint> Words { get; }

    public ReferenceTerm(string node, ulong id, uint creation = 0) : this(node, id > uint.MaxValue ? [(uint)id, (uint)(id >> ReferenceIdentityLayout.WordBits)] : [(uint)id], creation)
    {
    }

    public ReferenceTerm(string node, IEnumerable<uint> words, uint creation = 0)
    {
        uint[] values = words.ToArray();
        if (values.Length < ReferenceIdentityLayout.MinimumWords || values.Length > ReferenceIdentityLayout.MaximumWords)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        externalIdentity = new(node, creation);
        Words = Array.AsReadOnly(values);
        BigInteger id = BigInteger.Zero;
        for (int index = values.Length - 1; index >= 0; index--)
            id = (id << ReferenceIdentityLayout.WordBits) | values[index];
        FullId = id;
    }

    internal ReferenceTerm(LocalNodeIdentity identity, IEnumerable<uint> words)
        : this(identity.Snapshot.Node, words, identity.Snapshot.Creation)
    {
        localIdentity = identity;
    }

    internal ReferenceTerm(ReferenceTerm identity)
        : this(identity.Node, identity.Words, identity.Creation)
    {
        localIdentity = identity.LocalIdentity;
    }

    public override string ToString()
    {
        var wire = WireIdentity;

        return $"#Ref<{wire.Node}.{FullId}.{wire.Creation}>";
    }
}
