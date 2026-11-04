namespace Erlang;

// Diagnostic values only: no dereferenceable buffer or guest authority escapes.
internal readonly record struct RuntimeMapSourceSortEvent(
    RuntimeMapSourceSortKind Kind,
    ulong BufferAddress,
    ulong ComparerAddress,
    int Start,
    int Count,
    uint Seed,
    int Pivot
);
