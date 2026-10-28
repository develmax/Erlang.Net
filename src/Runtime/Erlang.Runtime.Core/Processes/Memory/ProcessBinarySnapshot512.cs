namespace Erlang;

// Actual managed backing capacities retained by explicit private root leases.
// These are bytes/ref leases, not ERTS heap words, ProcBins or public metrics.
internal readonly record struct ProcessBinarySnapshot512(
    long ReferenceBackingBytes,
    long HeapBackingBytes,
    int DistinctBackings,
    long RootBackingReferences,
    int RootFrames
);
