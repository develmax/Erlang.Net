namespace Erlang;

// Initial defaults must come from the logical VM configuration, never guessed
// from CLR process totals or inherited from the spawning process's overrides.
internal readonly record struct ProcessResourcePolicy508(
    ulong MinimumHeapWords,
    ulong MinimumBinaryHeapWords,
    ushort FullSweepAfter
);
