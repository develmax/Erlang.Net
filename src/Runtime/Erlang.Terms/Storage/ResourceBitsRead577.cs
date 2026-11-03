namespace Erlang;

// A validated per-read snapshot derived from actual SubBits/BinRef cells.
// Offset is rebased to the already-admitted native data range, not CLR state.
internal readonly record struct ResourceBitsRead577(long BitOffset, int BitLength);
