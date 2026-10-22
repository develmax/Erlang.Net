namespace Erlang;

internal static class AllocatorGatherNames434
{
    internal const string Module = "erts_internal", Histograms = "gather_alloc_histograms", Carriers = "gather_carrier_info";
    internal const string System = "system", NifType = "nif_internal", AtomicsType = "erl_bif_atomics", CountersType = "erl_bif_counters", LiteralType = "literal", PersistentType = "persistent_term";
    internal const string Arity = "Allocator gather native argument count is inconsistent";
    internal const string Membership = "Native allocation window owner membership is inconsistent", Foreign = "Allocator scan requires its actual owning runtime", Conflict = "Allocator scan publication conflicts with an existing entry";
    internal const string Frontier = "Native driver child/binary allocation attribution is not yet represented by this owner profile";
    internal const string Profile = "Actual VM-published native NIF/atomics/counters/persistent allocation spans; one independently owned native block per carrier; no CLR/CRT hidden overhead, ERTS bins or guessed physical pages";
    internal static readonly (string Function, int Arity)[] Signatures = [(Histograms, 1), (Carriers, 1)];
    internal static readonly IReadOnlySet<string> Allocators = new HashSet<string>{AllocatorBackendNames.System,AllocatorBackendNames.Temporary,
        AllocatorBackendNames.ShortLived,AllocatorBackendNames.Standard,AllocatorBackendNames.LongLived,AllocatorBackendNames.ProcessHeap,
        AllocatorBackendNames.Ets,AllocatorBackendNames.FixedSize,AllocatorBackendNames.Literal,AllocatorBackendNames.Binary,AllocatorBackendNames.Driver,AllocatorBackendNames.Test};
}
