namespace Erlang;

// All arrays, chain validation and pointer arithmetic precede commit. Successful
// GC leaves physical range release to the common collector; rollback owns it.
internal sealed class MagicReferenceSweep576(
    ProcessTermHeap506 owner,
    NativeOffheapHeader576[] dead,
    NativeOffheapHeader576[] alive,
    nuint[][] links,
    bool rollback
)
{
    internal int RegularCount { get; init; } = alive.Length;

    internal void Commit576() => owner.CommitMagicChains(
        dead,
        alive,
        links,
        rollback,
        RegularCount
    );
}
