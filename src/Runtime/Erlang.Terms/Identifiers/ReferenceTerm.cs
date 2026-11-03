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
    private readonly BigInteger portableId562;
    private readonly IReadOnlyList<uint> portableWords562 = Array.Empty<uint>();
    private readonly bool ordinaryLocal562;
    internal IOwnedReferenceStorage562? OwnedStorage562 { get; }
    internal bool IsOrdinaryLocal562 => ordinaryLocal562;
    public BigInteger FullId => OwnedStorage562 is null ? portableId562 : Reconstruct562(Words);
    public uint Creation => WireIdentity.Creation;
    public IReadOnlyList<uint> Words => OwnedStorage562?.ReadReferenceWords562() ?? portableWords562;

    public ReferenceTerm(string node, ulong id, uint creation = 0) : this(node, id > uint.MaxValue ? [(uint)id, (uint)(id >> ReferenceIdentityLayout.WordBits)] : [(uint)id], creation)
    {
    }

    public ReferenceTerm(string node, IEnumerable<uint> words, uint creation = 0)
    {
        uint[] values = words.ToArray();
        if (values.Length < ReferenceIdentityLayout.MinimumWords || values.Length > ReferenceIdentityLayout.MaximumWords)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        externalIdentity = new(node, creation);
        portableWords562 = Array.AsReadOnly(values);
        BigInteger id = BigInteger.Zero;
        for (int index = values.Length - 1; index >= 0; index--)
            id = (id << ReferenceIdentityLayout.WordBits) | values[index];
        portableId562 = id;
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
        ordinaryLocal562 = identity.ordinaryLocal562;
    }

    // Only the runtime's ordinary make_ref generator uses this factory. Public
    // constructors, decoder paths and PID/resource aliases receive no capability.
    internal static ReferenceTerm CreateOrdinaryLocal562(LocalNodeIdentity identity, uint[] words)
        => new(identity, words, true);

    private ReferenceTerm(LocalNodeIdentity identity, uint[] words, bool ordinary)
        : this(identity, words)
    {
        if (words.Length != ReferenceIdentityLayout.LocalWords)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        ordinaryLocal562 = ordinary;
    }

    internal ReferenceTerm(LocalNodeIdentity identity, IOwnedReferenceStorage562 storage)
    {
        externalIdentity = identity.Snapshot;
        localIdentity = identity;
        OwnedStorage562 = storage;
        ordinaryLocal562 = true;
    }

    private static BigInteger Reconstruct562(IReadOnlyList<uint> words)
    {
        BigInteger id = BigInteger.Zero;
        for (int index = words.Count - 1; index >= 0; index--)
            id = (id << ReferenceIdentityLayout.WordBits) | words[index];

        return id;
    }

    public override string ToString()
    {
        var wire = WireIdentity;

        return $"#Ref<{wire.Node}.{FullId}.{wire.Creation}>";
    }
}
