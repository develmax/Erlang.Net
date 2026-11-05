namespace Erlang;

public sealed partial class ProcessContext
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot)
    {
        Term owner = NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Process, Self);
        // Scan physical MSO first: dead-but-not-collected headers count too.
        termHeap506?.CaptureNodeDistReferences(snapshot, owner, NodeDistReferenceNames.Heap);
        foreach (Term value in Dictionary.CaptureNodeDistReferences())
            snapshot.Graph(value, owner, NodeDistReferenceNames.Heap);
        using NodeDistMailboxRead messages = Mailbox.CaptureNodeDistReferences();
        messages.Capture(snapshot, owner);
    }
}
