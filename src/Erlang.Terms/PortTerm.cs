using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class PortTerm : Term
{
    private readonly LocalNodeIdentity? localIdentity;
    private readonly LocalNodeSnapshot externalIdentity;
    public string Node => WireIdentity.Node;
    public ulong Id { get; }
    public uint Creation => WireIdentity.Creation;
    internal LocalNodeSnapshot WireIdentity => localIdentity?.Snapshot ?? externalIdentity;
    internal LocalNodeIdentity? LocalIdentity => localIdentity;

    public PortTerm(string node, ulong id, uint creation = 0)
    {
        externalIdentity = new(node, creation);
        Id = id;
    }

    internal PortTerm(LocalNodeIdentity identity, ulong id)
    {
        localIdentity = identity;
        externalIdentity = identity.Snapshot;
        Id = id;
    }

    public override string ToString()
    {
        var wire = WireIdentity;

        return $"#Port<{wire.Node}.{Id}.{wire.Creation}>";
    }
}
