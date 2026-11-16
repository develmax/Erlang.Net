namespace Erlang;

internal sealed partial class RuntimeEtsStoredPayload
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot, Term owner)
    {
        var identities = new HashSet<RuntimeExternalTermReservation>(ReferenceEqualityComparer.Instance);
        if (Volatile.Read(ref compressed438) is { } compressed)
        {
            // Compressed bodies are wire data; only the uncompressed key
            // contributes external-identifier headers to the ETS offheap list.
            foreach (var node in compressed.KeyNodes)
                if (node.Identity is { } identity) identities.Add(identity);
        }
        else
        {
            foreach (var node in Volatile.Read(ref nodes) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredPayload)))
                if (node.Identity is { } identity) identities.Add(identity);
        }
        foreach (var identity in identities)
        {
            // A copied ETS identity is an independent stored reference even if
            // its public DTO happened to be shared with another stored row.
            Term value = identity.Value;
            LocalNodeSnapshot? epoch = value switch
            {
                Pid { LocalIdentity: null } pid => pid.WireIdentity,
                PortTerm { LocalIdentity: null } port => port.WireIdentity,
                ReferenceTerm { LocalIdentity: null } reference => reference.WireIdentity,
                _ => null
            };
            if (epoch is { } external) snapshot.Node(
                external,
                owner,
                NodeDistReferenceNames.Ets,
                identity
            );
            else snapshot.Graph(value, owner, NodeDistReferenceNames.Ets);
        }
    }
}
