using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed partial class Pid : Term
{
    private readonly LocalNodeIdentity? localIdentity;
    private readonly LocalNodeSnapshot externalIdentity;
    public string Node => WireIdentity.Node;
    private readonly ulong portableId;
    private readonly IOwnedExternalIdentifierStorage? externalStorage;
    internal IOwnedExternalIdentifierStorage? OwnedExternalStorage => externalStorage;
    public ulong Id => externalStorage?.ReadExternalId() ?? portableId;
    public uint Creation => WireIdentity.Creation;
    internal LocalNodeSnapshot WireIdentity => externalStorage?.ReadExternalIdentity() ?? localIdentity?.Snapshot ?? externalIdentity;
    internal LocalNodeIdentity? LocalIdentity => localIdentity;

    public Pid(string node, ulong id, uint creation = 0)
    {
        externalIdentity = new(node, creation);
        portableId = id;
    }

    internal Pid(LocalNodeIdentity identity, ulong id)
    {
        localIdentity = identity;
        externalIdentity = identity.Snapshot;
        portableId = id;
    }

    internal Pid(IOwnedExternalIdentifierStorage storage)
    {
        externalStorage = storage;
        externalIdentity = storage.ReadExternalIdentity();
    }

    public override string ToString()
    {
        var wire = WireIdentity;

        return $"<{wire.Node}.{Id}.{wire.Creation}>";
    }
}
