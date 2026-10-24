namespace Erlang.Tests;

internal readonly record struct NifAllocationDomainHostSnapshot(
    int JoinedOwners,
    NifAllocationLedgerHostSnapshot Allocations
);
