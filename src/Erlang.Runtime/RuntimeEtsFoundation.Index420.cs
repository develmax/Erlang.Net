namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private ulong CoreHash420(Term key)
    {
        if (key is Atom atom) return ErlangNativeHash.Atom(atom.Name) >> 1;
        using var identity = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);

        return identity.Hash(key) >> 1;
    }

    private static IEnumerable<RuntimeEtsObjectSlot> CoreRows420(RuntimeEtsFoundationTable table)
     => table.Primary420 is { } index ? index.LiveRows : table.ObjectOrder;

    private EtsKeyGroup420? CoreKeyGroup420(RuntimeEtsFoundationTable table, Term key, CancellationToken cancellation)
    {
        var index = table.Primary420 ?? throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);

        return index.Find(
            key,
            index.Kind == EtsCoreTableKind407.OrderedSet ? 0 : CoreHash420(key),
            group => CoreReadKey438(
                group.KeyPayload,
                coreOptions407.GetValue(table, _ => throw new InvalidOperationException(EtsIndexNames420.InvalidIndex)).KeyPosition,
                cancellation
            )
        );
    }

    private EtsIndexChange420 CoreStageIndex420(
        RuntimeEtsFoundationTable table,
        IEnumerable<RuntimeEtsObjectSlot> live,
        IEnumerable<RuntimeEtsStoredObject> retirement,
        CancellationToken cancellation,
        bool? fixedTable = null
    )
    {
        var options = coreOptions407.GetValue(table, _ => throw new InvalidOperationException(EtsIndexNames420.InvalidIndex));
        EtsPrimaryIndex420 index = EtsPrimaryIndex420.Build(
            table.Primary420,
            live,
            options,
            fixedTable ?? table.FixedCount420 != 0,
            payload => CoreReadKey438(payload, options.KeyPosition, cancellation),
            CoreHash420,
            cancellation
        );
        var kept = new HashSet<RuntimeEtsStoredObject>(index.PhysicalRows.Select(slot => slot.Payload), ReferenceEqualityComparer.Instance);
        var retired = new HashSet<RuntimeEtsStoredObject>(ReferenceEqualityComparer.Instance);
        foreach (var payload in retirement) if (!kept.Contains(payload)) retired.Add(payload);
        if (table.Primary420 is { } old) foreach (var group in old.Groups) if (group.Tombstone is { } dead && !kept.Contains(dead.Payload)) retired.Add(dead.Payload);

        return new(index, retired.ToArray());
    }

    private static void CorePublishIndex420(RuntimeEtsFoundationTable table, EtsIndexChange420 change)
    {
        table.Primary420 = change.Index;
        table.Objects = change.Index.LiveObjects;
        table.ObjectOrder = change.Index.PhysicalRows;
    }
}
