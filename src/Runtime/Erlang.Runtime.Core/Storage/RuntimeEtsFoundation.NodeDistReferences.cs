namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal void CaptureNodeDistReferences(NodeDistReferenceSnapshot snapshot)
    {
        var captured = new List<(Term Owner, RuntimeEtsStoredObject Payload, RuntimeEtsReadLease Lease)>();
        try
        {
            lock (gate)
            {
                foreach (var table in tables.Values)
                {
                    Term owner = NodeDistReferenceSnapshot.Owner(NodeDistReferenceNames.Ets, table.Named ? table.Name : table.Handle);
                    var unique = new HashSet<RuntimeEtsStoredObject>(ReferenceEqualityComparer.Instance);
                    foreach (var row in table.Primary420?.PhysicalRows ?? table.ObjectOrder)
                        unique.Add(row.Payload);
                    if (table.Heir419?.Data is { } heir) unique.Add(heir);
                    foreach (var payload in unique) captured.Add((owner, payload, payload.Acquire()));
                }
            }
            foreach (var entry in captured) entry.Payload.CaptureNodeDistReferences(snapshot, entry.Owner);
        }
        finally
        {
            foreach (var entry in captured) entry.Lease.Dispose();
        }
    }
}
