namespace Erlang;

internal readonly record struct PersistentArenaAllocationReport416(
    long AllocatedPairBlocks,
    long AllocatedPairBytes,
    long AllocatedTableBlocks,
    long AllocatedTableBytes
);
