namespace Erlang.Tests;

internal readonly record struct NifAllocationLedgerHostSnapshot(
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
