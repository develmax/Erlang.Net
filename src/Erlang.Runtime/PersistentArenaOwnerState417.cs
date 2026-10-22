namespace Erlang;

internal readonly record struct PersistentArenaOwnerState417(
    PersistentArenaInfo416? Active,
    bool Closed,
    PersistentArenaAllocationReport416 Allocations
);
