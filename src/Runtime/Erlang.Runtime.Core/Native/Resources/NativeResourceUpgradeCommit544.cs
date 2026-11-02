namespace Erlang;

// Created before C can change old private data. Closing does not revoke this
// particular operation; commit into a closed image retires rather than names it.
internal sealed class NativeResourceUpgradeCommit544
{
    private readonly NativeResourceLoadTransaction527 transaction;
    private readonly NativeResourceTypeTable527 table;
    private readonly NativeResourcePublication527 publication;
    private int acquired544;

    private NativeResourceUpgradeCommit544(
        NativeResourceLoadTransaction527 transaction,
        NativeResourceTypeTable527 table,
        NativeResourcePublication527 publication
    )
    {
        this.transaction = transaction;
        this.table = table;
        this.publication = publication;
    }

    internal static NativeResourceUpgradeCommit544 Create544(
        NativeResourceLoadTransaction527 transaction,
        NativeResourceTypeTable527 table,
        NativeResourcePublication527 publication
    ) => new(transaction, table, publication);

    // Only the admitted native transition calls this after callback success.
    // Callback writer ownership starts here, after normal schedulers resume.
    internal NativeResourcePublication527 AcquireAfterSuccess544()
    {
        if (Interlocked.CompareExchange(ref acquired544, 1, 0) != 0)
            throw new InvalidOperationException(NativeResourceUpgradeSnapshotNames544.Repeated);
        transaction.EndLoading527();
        table.EnterPublication529();
        publication.Admit527();

        return publication;
    }
}
