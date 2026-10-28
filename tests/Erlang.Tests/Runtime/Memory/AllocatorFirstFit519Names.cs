using System.Reflection;

namespace Erlang.Tests;

internal static class AllocatorFirstFit519Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string SlabName = "Erlang.ProcessTermSlab506";
    internal const string HeapName = "Erlang.ProcessTermHeap506";
    internal const string Reserve = "Reserve";
    internal const string Release = "Release";
    internal const string AllocateRaw = "Allocate";
    internal const string HeapGate = "Gate";
    internal const string OwnedSequenceType = "Erlang.IOwnedTermSequence506";
    internal const string CellVisits = "ReservationCellVisits519";
    internal const string SlabVisits = "AllocationSlabVisits519";
    internal const string Storage = "OwnedStorage506";
    internal const string Slab = "Slab";
    internal const string Offset = "Offset";
    internal const string Identity = "identity";
    internal const string Enter = "Enter";
    internal const string Borrow = "Borrow";
    internal const string Collect = "CollectGeneration508";
    internal const string Retire = "RetireAsync";
    internal const string MixedTest = "runtime/allocator519/mixed-exact-first-fit";
    internal const string FragmentedTest = "runtime/allocator519/fragmented-linear-bitmap-scan";
    internal const string HeapTest = "runtime/allocator519/linear-heap-slab-search";
    internal const string ReuseTest = "runtime/allocator519/collection-and-rollback-reuse";
    internal const string BitmapDiagnostic = "Allocator519 actual bitmap cells={0}, allocations={1}";
    internal const string SlabDiagnostic = "Allocator519 actual slab visits={0}, allocations={1}";
}
