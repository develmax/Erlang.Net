namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    // Caller holds the actual foundation gate. The extent function must be an authenticated internal allocator capability, never a mutable HOST callback.
    private static EtsOwnedCensus420 CoreOwnedCensus420(
        RuntimeEtsFoundationTable table,
        RuntimeEtsCoreOptions407 options,
        Func<object, nuint> allocatedExtent,
        CancellationToken cancellation
    )
    {
        var census = new EtsOwnedCensus420(allocatedExtent, cancellation);
        census.Add(table);
        census.Add(options);
        census.Add(table.OwnerLink);
        census.Add(table.CreationLink420);
        census.Integer(table.FixedAtMonotonic420);
        census.Integer(table.FixedOffset420);
        census.Dictionary(table.Objects);
        census.List(table.ObjectOrder);
        foreach (var slot in table.ObjectOrder)
        {
            census.Add(slot);
            census.Add(slot.Link);
            slot.Payload.AddOwnedCensus420(census);
        }
        if (table.Primary420 is { } index)
        {
            census.Add(index);
            census.Array(index.Buckets);
            foreach (var bucket in index.Buckets) census.Array(bucket);
            census.Array(index.Ordered);
            census.Array(index.Groups);
            census.Array(index.LiveRows);
            census.Dictionary(index.LiveObjects);
            census.List(index.PhysicalRows);
            foreach (var group in index.Groups)
            {
                census.Add(group);
                census.Array(group.Rows);
                foreach (var slot in group.Rows)
                {
                    census.Add(slot);
                    census.Add(slot.Link);
                    slot.Payload.AddOwnedCensus420(census);
                }
                if (group.Tombstone is { } dead)
                {
                    census.Add(dead);
                    census.Add(dead.Link);
                    dead.Payload.AddOwnedCensus420(census);
                }
            }
        }
        census.Dictionary(table.Fixations420);
        census.List(table.FixedOrder420);
        foreach (var entry in table.Fixations420)
        {
            var fix = entry.Value;
            census.Add(fix);
            census.Add(fix.TableLink);
            census.Add(fix.ProcessLink);
        }
        if (table.Heir419 is { } heir)
        {
            census.Add(heir);
            census.Add(heir.Generation);
            heir.Data?.AddOwnedCensus420(census);
        }

        // Opaque TID entry/identity, shared owner/process/lane/name registries and source factories are owned by their existing runtime authorities, not charged repeatedly to every table.
        // Staged copies and detached retire/read-held roots are not in the active table ownership graph.
        return census;
    }
}
