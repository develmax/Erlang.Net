namespace Erlang;

internal sealed class OwnedWordSizeFrame526(ProcessTermSequence506 payload)
{
    internal ProcessTermSequence506 Payload { get; } = payload;
    internal IReadOnlyList<Term> Children { get; init; } = payload;
    internal int Next { get; set; }
    internal ulong Words { get; set; } = checked((ulong)payload.AllocationWords526);
}
