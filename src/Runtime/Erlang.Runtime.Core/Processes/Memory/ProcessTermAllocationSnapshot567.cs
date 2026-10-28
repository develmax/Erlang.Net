namespace Erlang;

// Actual occupied cells in owned slabs, including headers and numeric payloads.
// Excludes free capacity and all domains not physically admitted by this backend.
internal readonly record struct ProcessTermAllocationSnapshot567(
    long NurseryAllocationWords,
    long SurvivorAllocationWords,
    long OldAllocationWords,
    long ReclaimedAllocationWords
);
