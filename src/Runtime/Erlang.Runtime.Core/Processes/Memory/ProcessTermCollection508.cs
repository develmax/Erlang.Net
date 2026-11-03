namespace Erlang;

internal readonly record struct ProcessTermCollection508(
    bool Major,
    long ReclaimedPayloadWords,
    long CompletedCollections,
    ushort MinorCollectionsSinceMajor
)
{
    // Actual occupied header/raw/child cells reclaimed from this private backend.
    // This is separate from pointer-child payload count and ERTS public metrics.
    internal long ReclaimedAllocationWords567 { get; init; }
}
