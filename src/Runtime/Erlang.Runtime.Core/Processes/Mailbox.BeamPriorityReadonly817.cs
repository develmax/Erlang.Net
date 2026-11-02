namespace Erlang;

public sealed partial class Mailbox
{
    // The sealed source validation has traversed the complete actual immutable
    // family and rejects ALL ReferenceTerm/mutable/native-record descendants.
    // This is not an exemption for arbitrary graphs or a cached guest root.
    private void BeforeBeamPriorityInsertReadonly817(ReadonlyTraceSource817 source)
    {
        source.RequireValidated();
        if (!ReferenceEquals(source.Receiver817.Mailbox, this) || !ReferenceEquals(source.Runtime, Runtime593))
            throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        if (beamSave is not { } save)
            return;
        if (lastPriority is null)
        {
            foreach (ReceiveMarkerState marker in receiveMarkers)
                marker.SeenInBeamPriority = false;
            if (save.Marker is { Retired: true } retired)
                retired.SeenInBeamPriority = false;
        }
        if (save.InPriority || save.Marker is { SeenInBeamPriority: false })
            return;
        if (save.PriorityContinuation is { } prior)
            RemoveNode(prior);
        LinkedListNode<Entry>? current = save.After is null ? messages.First : save.After.Next;
        var entry = new Entry(Nil.Value, null, Control: true);
        save.PriorityContinuation = current is null ? messages.AddLast(entry) : messages.AddBefore(current, entry);
        mutationRevision++;
        save.After = lastPriority;
        save.InPriority = true;
    }
}
