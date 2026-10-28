namespace Erlang;

internal readonly record struct ProcessTermHeapSnapshot506(
    long CapacityWords,
    long LivePayloadWords,
    int Blocks,
    int RootFrames
);
