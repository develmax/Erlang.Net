namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot, Term owner, string kind)
    {
        (object Header, LocalNodeSnapshot Epoch)[] entries;
        Term[] distributionHandles;
        lock (Gate)
        {
            entries = magicReferences576.Values.Where(entry => entry.ExternalNode is not null)
                .Select(entry => ((object)entry.Block, entry.ExternalNode!.Snapshot)).ToArray();
            distributionHandles = magicReferences576.Values.Where(entry => entry.MagicTerm?.Entry.Kind is ReferenceResourceKind.DistributionEntry or ReferenceResourceKind.ConnectedDistributionHandle)
                .Select(entry => (Term)entry.MagicTerm!).ToArray();
        }
        foreach (var entry in entries) snapshot.Node(
            entry.Epoch,
            owner,
            kind,
            entry.Header
        );
        foreach (Term handle in distributionHandles) snapshot.Graph(handle, owner, kind);
    }
}
