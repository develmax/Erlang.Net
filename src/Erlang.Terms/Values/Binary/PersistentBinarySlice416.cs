namespace Erlang;

internal readonly record struct PersistentBinarySlice416(
    PersistentBinaryBacking416? Backing,
    long BitOffset,
    int BitLength
);
