namespace Erlang;

internal readonly record struct NifNativeAllocationDomainSnapshot(
    int JoinedOwners,
    NifNativeAllocationSnapshot Allocations
);
