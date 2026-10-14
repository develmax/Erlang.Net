using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Pid : Term
{
    private readonly LocalNodeIdentity? localIdentity;
    private readonly LocalNodeSnapshot externalIdentity;
    public string Node => WireIdentity.Node;
    public ulong Id { get; }
    public uint Creation => WireIdentity.Creation;
    internal LocalNodeSnapshot WireIdentity => localIdentity?.Snapshot ?? externalIdentity;
    internal LocalNodeIdentity? LocalIdentity => localIdentity;

    public Pid(string node, ulong id, uint creation = 0)
    {
        externalIdentity = new(node, creation);
        Id = id;
    }

    internal Pid(LocalNodeIdentity identity, ulong id)
    {
        localIdentity = identity;
        externalIdentity = identity.Snapshot;
        Id = id;
    }

    public override string ToString()
    {
        var wire = WireIdentity;

        return $"<{wire.Node}.{Id}.{wire.Creation}>";
    }
}
