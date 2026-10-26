namespace Erlang;

internal readonly record struct PersistentArenaInfo416(
    int Count,
    long Memory,
    int Capacity,
    long ActualTableRequestedBytes,
    long ActivePairRequestedBytes
);
