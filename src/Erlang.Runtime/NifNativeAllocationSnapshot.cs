namespace Erlang;

internal readonly record struct NifNativeAllocationSnapshot(
    int Count,
    ulong RequestedBytes,
    ulong BackingRequestBytes,
    ulong PeakRequestedBytes,
    ulong PeakBackingRequestBytes,
    ulong AllocatedRequestedBytes,
    ulong ReleasedRequestedBytes,
    ulong AllocatedBackingRequestBytes,
    ulong ReleasedBackingRequestBytes,
    long SuccessfulAllocations,
    long ReleasedAllocations
);
