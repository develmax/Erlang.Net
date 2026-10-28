namespace Erlang;

internal readonly record struct ProcessTermCollection508(
    bool Major,
    long ReclaimedPayloadWords,
    long CompletedCollections,
    ushort MinorCollectionsSinceMajor
);
