namespace Erlang;

internal sealed class EtsCursorState420
{
    internal Term Tid { get; init; } = Nil.Value;
    internal long Slot { get; init; }
    internal long Chunk { get; init; }
    internal Term LastKey { get; init; } = Nil.Value;
    internal Term EndCondition { get; init; } = Nil.Value;
    internal Term Accumulator { get; init; } = Nil.Value;
    internal long Count { get; init; }
    internal long Reverse { get; init; }
    internal ReferenceTerm? Reference { get; set; }
    internal MatchSpecProgram Program { get; init; } = null!;
    internal bool Initial { get; init; }
    internal bool BoundSingle { get; init; }
    internal EtsCursorBounds420? Bounds { get; init; }
}
