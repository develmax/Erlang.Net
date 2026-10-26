namespace Erlang;

// Exact owned-array payload accounting only; this type is deliberately not
// the native info/0 {count,memory} contract and is never guest-visible.
internal readonly record struct PersistentTermOwnedStorageReport(
    int ActivePairs,
    long ActiveReferenceArrayPayloadBytes,
    long RetainedPairPayloads,
    long RetainedPairArrayPayloadBytes,
    bool Closed
);
