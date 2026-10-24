namespace Erlang.Tests;

internal static class NifAllocationLedgerHostNames
{
    internal const string Test = "runtime/nif-native-requested-backing-allocation-ledger";
    internal const string OwnerType = "Erlang.NifNativeAllocationOwner";
    internal const string RuntimeAssembly = "Erlang.Runtime";
    internal const string Snapshot = "Snapshot";
    internal const string SnapshotField = "snapshot";
    internal const string Count = "Count";
    internal const string Allocate = "Allocate";
    internal const string Free = "Free";
    internal const string ReclaimAll = "ReclaimAll";
    internal const string ForeignAllocationFree = "Foreign or duplicate native free.";
    internal const string ReflectionContract = "Actual Runtime allocation owner reflection contract is missing.";
    internal const string Broken = "Actual native allocation ledger invariant failed.";
    internal const string ForeignAccepted = "Foreign free was accepted.";
    internal const string OverflowAccepted = "Counter overflow was accepted.";
    internal const string RealPayloadCase = "real-payload-zero-backing-immutable-snapshot-and-free";
    internal const string ReclamationCase = "double-free-post-reclaim-idempotent-cleanup-and-reuse";
    internal const string OwnerSeparationCase = "separate-real-owners-refuse-foreign-pointer";
    internal const string OverflowCase = "checked-cumulative-overflow-before-allocation-with-no-ledger-mutation";
    internal const string ConcurrentSnapshotCase = "concurrent-real-alloc-free-snapshot-and-lifecycle-serialized-reclamation";
    internal const string FocusedRuntimeStatus = "ACTUAL_RUNTIME_DLL_ALLOCATION_LEDGER_HOST_PASS";
}
