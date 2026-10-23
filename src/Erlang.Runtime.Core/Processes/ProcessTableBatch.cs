namespace Erlang;

internal sealed record ProcessTableBatch(int Cursor, IReadOnlyList<Pid> Processes);
