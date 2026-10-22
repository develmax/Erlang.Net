namespace Erlang;

// Requested native allocation extents, never mislabeled as managed-array bytes.
internal readonly record struct PersistentTermArenaReport417(
    int ActivePairs,
    long ActiveTableRequestedBytes,
    long ActivePairRequestedBytes,
    long RetainedPairPayloads,
    long RetainedPairNativeBytes,
    long RetainedTableBlocks,
    long RetainedTableNativeBytes,
    bool Closed
);
