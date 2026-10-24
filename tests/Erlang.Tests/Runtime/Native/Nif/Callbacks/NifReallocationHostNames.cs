using System.Reflection;

namespace Erlang.Tests;

internal static class NifReallocationHostNames
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Module = "nif_reallocation_host_wrong_module";
    internal const string Function = "fallback";
    internal const string Test = "runtime/nif-real-payload-reallocation-extent-history";
    internal const string Reallocate = "Reallocate";
    internal const string SnapshotStorage = "snapshot";
    internal const string Backend = "backend";
    internal const string Domain = "NativeAllocationDomain";
    internal const string Allocate = "Allocate";
    internal const string Free = "Free";
    internal const string Reclaim = "ReclaimAll";
    internal const string Foreign = "Foreign or stale native reallocation.";
    internal const string Broken = "Actual native reallocation extent invariant failed.";
    internal const string OverflowAccepted = "Synthetic counter overflow was accepted before native resize.";
    internal const string FocusStatus = "ACTUAL_RUNTIME_REALLOCATION_HOST_PASS";
}
