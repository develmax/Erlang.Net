namespace Erlang;

internal sealed record BeamTypeTable(bool Fallback, int? WireVersion, IReadOnlyList<BeamTypeEntry> Entries);
