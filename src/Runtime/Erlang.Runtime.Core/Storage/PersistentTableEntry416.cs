namespace Erlang;

// A temporary staging descriptor, never a second key/value graph or lookup table.
internal readonly record struct PersistentTableEntry416(ulong Hash, PersistentPairArena416 Area, int Slot = -1);
